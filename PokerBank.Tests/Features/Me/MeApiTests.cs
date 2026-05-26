using System.Net;
using System.Net.Http.Json;
using PokerBank.Domain;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Me;

[Collection(ApiTestCollection.Name)]
public sealed class MeApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMyBalance_ReturnsLinkedPlayerBalance()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var game = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, game.Id, lorenzo.Id, 100m);
        await AddBuyIn(ownerClient, game.Id, maya.Id, 100m);
        await AddCashOut(ownerClient, game.Id, lorenzo.Id, 160m);
        await AddCashOut(ownerClient, game.Id, maya.Id, 40m);
        await CloseGame(ownerClient, game.Id);

        await RecordPayment(ownerClient, lorenzo.Id, 20m, "ReceivedByPlayer");

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync("/me/balance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        Assert.NotNull(balance);
        Assert.Equal(lorenzo.Id, balance.PlayerId);
        Assert.Equal("Lorenzo", balance.PlayerName);
        Assert.Equal(60m, balance.GameNetAmount);
        Assert.Equal(20m, balance.PaymentNetAmount);
        Assert.Equal(40m, balance.BalanceAmount);
    }

    [Fact]
    public async Task GetMyBalance_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/me/balance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListBalances_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/balances");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Create game response was empty.");
    }

    private static async Task AddBuyIn(HttpClient client, Guid gameId, Guid playerId, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/buy-ins",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task AddCashOut(HttpClient client, Guid gameId, Guid playerId, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/cash-outs",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task CloseGame(HttpClient client, Guid gameId)
    {
        var response = await client.PostAsync($"/games/{gameId}/close", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task RecordPayment(HttpClient client, Guid playerId, decimal amount, string direction)
    {
        var response = await client.PostAsJsonAsync(
            PaymentUrl(playerId, direction),
            new { Amount = amount, Method = "ETransfer" });
        response.EnsureSuccessStatusCode();
    }

    private static string PaymentUrl(Guid playerId, string direction) =>
        direction switch
        {
            "MadeByPlayer" => $"/players/{playerId}/payments/made",
            "ReceivedByPlayer" => $"/players/{playerId}/payments/received",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown payment direction.")
        };

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record BalanceResponse(
        Guid PlayerId,
        string PlayerName,
        bool IsActive,
        decimal GameNetAmount,
        decimal PaymentNetAmount,
        decimal BalanceAmount);
}
