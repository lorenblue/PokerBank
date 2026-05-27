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
    public async Task GetMyGames_ReturnsGamesForLinkedPlayer()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var olderGame = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, olderGame.Id, lorenzo.Id, 100m);
        await AddBuyIn(ownerClient, olderGame.Id, maya.Id, 50m);
        await AddCashOut(ownerClient, olderGame.Id, lorenzo.Id, 40m);
        await AddCashOut(ownerClient, olderGame.Id, maya.Id, 110m);
        await CloseGame(ownerClient, olderGame.Id);

        await Task.Delay(10);

        var unrelatedGame = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, unrelatedGame.Id, maya.Id, 75m);
        await AddCashOut(ownerClient, unrelatedGame.Id, maya.Id, 75m);
        await CloseGame(ownerClient, unrelatedGame.Id);

        await Task.Delay(10);

        var newerGame = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, newerGame.Id, lorenzo.Id, 80m);
        await AddCashOut(ownerClient, newerGame.Id, lorenzo.Id, 25m);

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync("/me/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<MyGameResponse[]>();

        Assert.NotNull(games);
        Assert.Collection(
            games,
            game =>
            {
                Assert.Equal(newerGame.Id, game.Id);
                Assert.Equal("Open", game.Status);
                AssertCloseTo(newerGame.CreatedAtUtc, game.PlayedAtUtc);
                Assert.Equal(80m, game.MyBuyInAmount);
                Assert.Equal(25m, game.MyCashOutAmount);
                Assert.Equal(-55m, game.MyNetAmount);
                Assert.Equal(1, game.PlayerCount);
                Assert.Equal(80m, game.TotalBuyInAmount);
                Assert.Equal(25m, game.TotalCashOutAmount);
            },
            game =>
            {
                Assert.Equal(olderGame.Id, game.Id);
                Assert.Equal("Closed", game.Status);
                AssertCloseTo(olderGame.CreatedAtUtc, game.PlayedAtUtc);
                Assert.Equal(100m, game.MyBuyInAmount);
                Assert.Equal(40m, game.MyCashOutAmount);
                Assert.Equal(-60m, game.MyNetAmount);
                Assert.Equal(2, game.PlayerCount);
                Assert.Equal(150m, game.TotalBuyInAmount);
                Assert.Equal(150m, game.TotalCashOutAmount);
            });
    }

    [Fact]
    public async Task GetMyGames_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/me/games");

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

    [Fact]
    public async Task ListGames_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/games");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListPlayers_ReturnsForbidden_ForMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/players");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayer_ReturnsForbidden_ForMember()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var player = await CreatePlayer(ownerClient, "Lorenzo");

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync($"/players/{player.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ManagerOnlyEndpointCases))]
    public async Task ManagerOnlyEndpoint_ReturnsForbidden_ForMember(
        HttpMethod method,
        string path,
        object? body)
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static TheoryData<HttpMethod, string, object?> ManagerOnlyEndpointCases()
    {
        var playerId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        return new TheoryData<HttpMethod, string, object?>
        {
            { HttpMethod.Post, "/games", null },
            { HttpMethod.Delete, $"/games/{gameId}", null },
            { HttpMethod.Post, $"/games/{gameId}/buy-ins", new { PlayerId = playerId, Amount = 50m } },
            { HttpMethod.Post, $"/games/{gameId}/cash-outs", new { PlayerId = playerId, Amount = 50m } },
            { HttpMethod.Post, $"/games/{gameId}/close", null },
            { HttpMethod.Delete, $"/games/{gameId}/entries/{entryId}", null },
            { HttpMethod.Post, "/players", new { Name = "Lorenzo" } },
            { HttpMethod.Put, $"/players/{playerId}", new { Name = "Lorenzo", EmailAddress = "lorenzo@example.com" } },
            { HttpMethod.Post, $"/players/{playerId}/archive", null },
            { HttpMethod.Post, $"/players/{playerId}/payments/made", new { Amount = 20m, Method = "ETransfer" } },
            { HttpMethod.Post, $"/players/{playerId}/payments/received", new { Amount = 20m, Method = "ETransfer" } },
            { HttpMethod.Get, $"/payments/{paymentId}", null },
            { HttpMethod.Delete, $"/payments/{paymentId}", null }
        };
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

    private sealed record MyGameResponse(
        Guid Id,
        string Status,
        DateTime PlayedAtUtc,
        decimal MyBuyInAmount,
        decimal MyCashOutAmount,
        decimal MyNetAmount,
        int PlayerCount,
        decimal TotalBuyInAmount,
        decimal TotalCashOutAmount);

    private sealed record PaymentResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Direction,
        string Method,
        DateTimeOffset RecordedAtUtc);

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
