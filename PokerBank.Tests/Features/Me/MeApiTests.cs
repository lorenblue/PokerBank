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
    public async Task GetMyProfile_ReturnsLinkedPlayerProfile()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var player = await ownerClient.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

        await factory.LinkDefaultAdminToPlayerAsync(player.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<MyProfileResponse>();

        Assert.NotNull(profile);
        Assert.Equal(player.Id, profile.Id);
        Assert.Equal("Lorenzo", profile.Name);
        Assert.Equal("lorenzo@example.com", profile.EmailAddress);
        Assert.True(profile.IsActive);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsNotFound_WhenUserIsNotLinkedToPlayer()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyBalance_ReturnsLinkedPlayerBalance()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await ownerClient.CreatePlayerAsync("Lorenzo");
        var maya = await ownerClient.CreatePlayerAsync("Maya");

        var game = await ownerClient.CreateGameAsync();
        await ownerClient.AddBuyInAsync(game.Id, lorenzo.Id, 100m);
        await ownerClient.AddBuyInAsync(game.Id, maya.Id, 100m);
        await ownerClient.AddCashOutAsync(game.Id, lorenzo.Id, 160m);
        await ownerClient.AddCashOutAsync(game.Id, maya.Id, 40m);
        await ownerClient.CloseGameAsync(game.Id);

        await ownerClient.RecordPaymentAsync(lorenzo.Id, 20m, "ReceivedByPlayer");

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

        var lorenzo = await ownerClient.CreatePlayerAsync("Lorenzo");
        var maya = await ownerClient.CreatePlayerAsync("Maya");

        var olderPayment = await ownerClient.RecordPaymentAsync(lorenzo.Id, 20m, "MadeByPlayer");
        await Task.Delay(10);
        await ownerClient.RecordPaymentAsync(maya.Id, 50m, "ReceivedByPlayer");
        await Task.Delay(10);
        var newerPayment = await ownerClient.RecordPaymentAsync(lorenzo.Id, 15m, "ReceivedByPlayer");

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

        var lorenzo = await ownerClient.CreatePlayerAsync("Lorenzo");
        var maya = await ownerClient.CreatePlayerAsync("Maya");

        var olderGame = await ownerClient.CreateGameAsync();
        await ownerClient.AddBuyInAsync(olderGame.Id, lorenzo.Id, 100m);
        await ownerClient.AddBuyInAsync(olderGame.Id, maya.Id, 50m);
        await ownerClient.AddCashOutAsync(olderGame.Id, lorenzo.Id, 40m);
        await ownerClient.AddCashOutAsync(olderGame.Id, maya.Id, 110m);
        await ownerClient.CloseGameAsync(olderGame.Id);

        await Task.Delay(10);

        var unrelatedGame = await ownerClient.CreateGameAsync();
        await ownerClient.AddBuyInAsync(unrelatedGame.Id, maya.Id, 75m);
        await ownerClient.AddCashOutAsync(unrelatedGame.Id, maya.Id, 75m);
        await ownerClient.CloseGameAsync(unrelatedGame.Id);

        await Task.Delay(10);

        var newerGame = await ownerClient.CreateGameAsync();
        await ownerClient.AddBuyInAsync(newerGame.Id, lorenzo.Id, 80m);
        await ownerClient.AddCashOutAsync(newerGame.Id, lorenzo.Id, 25m);

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

        var player = await ownerClient.CreatePlayerAsync("Lorenzo");

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
            { HttpMethod.Put, $"/games/{gameId}/entries/{entryId}", new { Amount = 50m } },
            { HttpMethod.Delete, $"/games/{gameId}/entries/{entryId}", null },
            { HttpMethod.Post, "/players", new { Name = "Lorenzo" } },
            { HttpMethod.Put, $"/players/{playerId}", new { Name = "Lorenzo", EmailAddress = "lorenzo@example.com" } },
            { HttpMethod.Post, $"/players/{playerId}/archive", null },
            { HttpMethod.Post, $"/players/{playerId}/invite", null },
            { HttpMethod.Post, $"/players/{playerId}/payments/made", new { Amount = 20m, Method = "ETransfer" } },
            { HttpMethod.Post, $"/players/{playerId}/payments/received", new { Amount = 20m, Method = "ETransfer" } },
            { HttpMethod.Get, $"/payments/{paymentId}", null },
            { HttpMethod.Delete, $"/payments/{paymentId}", null }
        };
    }

    private static void AssertCloseTo(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }
}
