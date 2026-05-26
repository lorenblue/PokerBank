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
    public async Task GetMyPayments_ReturnsLinkedPlayerPayments()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var olderPayment = await RecordPayment(ownerClient, lorenzo.Id, 20m, "MadeByPlayer");
        await Task.Delay(10);
        await RecordPayment(ownerClient, maya.Id, 50m, "ReceivedByPlayer");
        await Task.Delay(10);
        var newerPayment = await RecordPayment(ownerClient, lorenzo.Id, 15m, "ReceivedByPlayer");

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync("/me/payments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payments = await response.Content.ReadFromJsonAsync<PaymentResponse[]>();

        Assert.NotNull(payments);
        Assert.Collection(
            payments,
            payment =>
            {
                Assert.Equal(newerPayment.Id, payment.Id);
                Assert.Equal(lorenzo.Id, payment.PlayerId);
                Assert.Equal(15m, payment.Amount);
                Assert.Equal("ReceivedByPlayer", payment.Direction);
            },
            payment =>
            {
                Assert.Equal(olderPayment.Id, payment.Id);
                Assert.Equal(lorenzo.Id, payment.PlayerId);
                Assert.Equal(20m, payment.Amount);
                Assert.Equal("MadeByPlayer", payment.Direction);
            });
    }

    [Fact]
    public async Task GetMyPayments_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/me/payments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyGameResults_ReturnsLinkedPlayerResults()
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

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync("/me/game-results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        var result = Assert.Single(results);
        Assert.Equal(lorenzo.Id, result.PlayerId);
        Assert.Equal("Lorenzo", result.PlayerName);
        Assert.Equal(game.Id, result.GameId);
        AssertCloseTo(game.CreatedAtUtc, result.PlayedAtUtc);
        Assert.Equal(100m, result.BuyInAmount);
        Assert.Equal(160m, result.CashOutAmount);
        Assert.Equal(60m, result.NetAmount);
    }

    [Fact]
    public async Task GetMyGameResults_FiltersByGame()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var firstGame = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, firstGame.Id, lorenzo.Id, 100m);
        await AddBuyIn(ownerClient, firstGame.Id, maya.Id, 100m);
        await AddCashOut(ownerClient, firstGame.Id, lorenzo.Id, 160m);
        await AddCashOut(ownerClient, firstGame.Id, maya.Id, 40m);
        await CloseGame(ownerClient, firstGame.Id);

        var secondGame = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, secondGame.Id, lorenzo.Id, 50m);
        await AddBuyIn(ownerClient, secondGame.Id, maya.Id, 50m);
        await AddCashOut(ownerClient, secondGame.Id, lorenzo.Id, 25m);
        await AddCashOut(ownerClient, secondGame.Id, maya.Id, 75m);
        await CloseGame(ownerClient, secondGame.Id);

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync($"/me/game-results?gameId={secondGame.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        var result = Assert.Single(results);
        Assert.Equal(lorenzo.Id, result.PlayerId);
        Assert.Equal(secondGame.Id, result.GameId);
        Assert.Equal(-25m, result.NetAmount);
    }

    [Fact]
    public async Task GetMyGameResults_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/me/game-results");

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

    [Fact]
    public async Task ListPayments_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/payments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListGameResults_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/game-results");

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

    private static async Task<PaymentResponse> RecordPayment(
        HttpClient client,
        Guid playerId,
        decimal amount,
        string direction)
    {
        var response = await client.PostAsJsonAsync(
            PaymentUrl(playerId, direction),
            new { Amount = amount, Method = "ETransfer" });
        response.EnsureSuccessStatusCode();

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        return payment ?? throw new InvalidOperationException("Create payment response was empty.");
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

    private sealed record PaymentResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Direction,
        string Method,
        DateTimeOffset RecordedAtUtc);

    private sealed record GameResultResponse(
        Guid PlayerId,
        string PlayerName,
        Guid GameId,
        DateTime PlayedAtUtc,
        decimal BuyInAmount,
        decimal CashOutAmount,
        decimal NetAmount);

    private sealed record BalanceResponse(
        Guid PlayerId,
        string PlayerName,
        bool IsActive,
        decimal GameNetAmount,
        decimal PaymentNetAmount,
        decimal BalanceAmount);

    private static void AssertCloseTo(DateTime expected, DateTime actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }
}
