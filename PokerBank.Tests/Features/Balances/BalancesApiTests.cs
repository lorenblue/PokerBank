using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Balances;

[Collection(ApiTestCollection.Name)]
public sealed class BalancesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListBalances_ReturnsPlayerBalances()
    {
        using var client = factory.CreateHttpsClient();

        var alice = await client.CreatePlayerAsync("Alice");
        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var closedGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(closedGame.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(closedGame.Id, maya.Id, 100m);
        await client.AddCashOutAsync(closedGame.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(closedGame.Id, maya.Id, 40m);
        await client.CloseGameAsync(closedGame.Id);

        var openGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(openGame.Id, lorenzo.Id, 999m);

        await client.RecordPaymentAsync(lorenzo.Id, 20m, "ReceivedByPlayer");
        await client.RecordPaymentAsync(maya.Id, 10m, "MadeByPlayer");

        var response = await client.GetAsync("/balances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        Assert.Collection(
            balances,
            balance =>
            {
                Assert.Equal(alice.Id, balance.PlayerId);
                Assert.Equal("Alice", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(0m, balance.GameNetAmount);
                Assert.Equal(0m, balance.PaymentNetAmount);
                Assert.Equal(0m, balance.BalanceAmount);
            },
            balance =>
            {
                Assert.Equal(lorenzo.Id, balance.PlayerId);
                Assert.Equal("Lorenzo", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(60m, balance.GameNetAmount);
                Assert.Equal(20m, balance.PaymentNetAmount);
                Assert.Equal(40m, balance.BalanceAmount);
            },
            balance =>
            {
                Assert.Equal(maya.Id, balance.PlayerId);
                Assert.Equal("Maya", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(-60m, balance.GameNetAmount);
                Assert.Equal(-10m, balance.PaymentNetAmount);
                Assert.Equal(-50m, balance.BalanceAmount);
            });
    }

    [Fact]
    public async Task ListBalances_IncludesArchivedPlayers()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo");
        await client.ArchivePlayerAsync(player.Id);

        var response = await client.GetAsync("/balances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        var balance = Assert.Single(balances);
        Assert.Equal(player.Id, balance.PlayerId);
        Assert.False(balance.IsActive);
    }

    [Fact]
    public async Task ListBalances_FiltersByPlayer()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var game = await client.CreateGameAsync();
        await client.AddBuyInAsync(game.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(game.Id, maya.Id, 100m);
        await client.AddCashOutAsync(game.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(game.Id, maya.Id, 40m);
        await client.CloseGameAsync(game.Id);

        await client.RecordPaymentAsync(lorenzo.Id, 20m, "ReceivedByPlayer");
        await client.RecordPaymentAsync(maya.Id, 10m, "MadeByPlayer");

        var response = await client.GetAsync($"/balances?playerId={lorenzo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        var balance = Assert.Single(balances);
        Assert.Equal(lorenzo.Id, balance.PlayerId);
        Assert.Equal("Lorenzo", balance.PlayerName);
        Assert.True(balance.IsActive);
        Assert.Equal(60m, balance.GameNetAmount);
        Assert.Equal(20m, balance.PaymentNetAmount);
        Assert.Equal(40m, balance.BalanceAmount);
    }

}
