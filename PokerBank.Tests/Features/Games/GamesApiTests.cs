using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokerBank.Api.Data;
using PokerBank.Domain;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Games;

[Collection(ApiTestCollection.Name)]
public sealed class GamesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateGame_ReturnsCreatedGame()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.NotNull(game);
        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal("Open", game.Status);
        Assert.NotEqual(default, game.CreatedAtUtc);
        Assert.Equal($"/games/{game.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateGame_ReturnsConflict_WhenOpenGameExists()
    {
        using var client = factory.CreateHttpsClient();

        await CreateGame(client);

        var response = await client.PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_AllowsOpenGameInDifferentPokerGroups()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        await CreateGame(otherGroupClient);

        var response = await client.PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ListGames_ReturnsGamesNewestFirst()
    {
        using var client = factory.CreateHttpsClient();

        var olderGame = await CreateGame(client);
        await CloseGame(client, olderGame.Id);
        await Task.Delay(10);
        var newerGame = await CreateGame(client);

        var response = await client.GetAsync("/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<GameResponse[]>();

        Assert.NotNull(games);
        Assert.Collection(
            games,
            game => Assert.Equal(newerGame.Id, game.Id),
            game => Assert.Equal(olderGame.Id, game.Id));
    }

    [Fact]
    public async Task ListGames_ReturnsOnlyGamesInCurrentPokerGroup()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        var currentGroupGame = await CreateGame(client);
        await CreateGame(otherGroupClient);

        var response = await client.GetAsync("/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<GameResponse[]>();

        Assert.NotNull(games);
        var game = Assert.Single(games);
        Assert.Equal(currentGroupGame.Id, game.Id);
    }


    [Fact]
    public async Task DeleteGame_ReturnsNoContent_WhenGameIsOpen()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.DeleteAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/games/{game.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGame_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        await CloseGame(client, game.Id);

        var response = await client.DeleteAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsGame_WhenGameExists()
    {
        using var client = factory.CreateHttpsClient();

        var createdGame = await CreateGame(client);

        var response = await client.GetAsync($"/games/{createdGame.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(game);
        Assert.Equal(createdGame.Id, game.Id);
        Assert.Equal(createdGame.Status, game.Status);
        AssertCloseTo(createdGame.CreatedAtUtc, game.CreatedAtUtc);
        Assert.Equal(0m, game.TotalBuyInAmount);
        Assert.Equal(0m, game.TotalCashOutAmount);
        Assert.Equal(0m, game.RemainingCashOutAmount);
        Assert.Empty(game.Entries);
        Assert.Empty(game.PlayerTotals);
    }

    [Fact]
    public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsGameForMemberWhoParticipated()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var game = await CreateGame(ownerClient);
        var lorenzoBuyIn = await AddBuyIn(ownerClient, game.Id, lorenzo.Id, 100m);
        var mayaBuyIn = await AddBuyIn(ownerClient, game.Id, maya.Id, 50m);

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal(game.Id, gameDetails.Id);
        Assert.Equal(150m, gameDetails.TotalBuyInAmount);
        Assert.Collection(
            gameDetails.Entries,
            entry =>
            {
                Assert.Equal(lorenzoBuyIn.Id, entry.Id);
                Assert.Equal(lorenzo.Id, entry.PlayerId);
                Assert.Equal(100m, entry.Amount);
                Assert.Equal("BuyIn", entry.Type);
            },
            entry =>
            {
                Assert.Equal(mayaBuyIn.Id, entry.Id);
                Assert.Equal(maya.Id, entry.PlayerId);
                Assert.Equal(50m, entry.Amount);
                Assert.Equal("BuyIn", entry.Type);
            });
        Assert.Collection(
            gameDetails.PlayerTotals,
            total =>
            {
                Assert.Equal(lorenzo.Id, total.PlayerId);
                Assert.Equal(-100m, total.NetAmount);
            },
            total =>
            {
                Assert.Equal(maya.Id, total.PlayerId);
                Assert.Equal(-50m, total.NetAmount);
            });
    }

    [Fact]
    public async Task GetGame_ReturnsForbiddenForMemberWhoDidNotParticipate()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(ownerClient, "Lorenzo");
        var maya = await CreatePlayer(ownerClient, "Maya");

        var game = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, game.Id, maya.Id, 50m);

        await factory.LinkDefaultAdminToPlayerAsync(lorenzo.Id);
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsForbiddenForMemberWithoutLinkedPlayer()
    {
        using var ownerClient = factory.CreateHttpsClient();

        var player = await CreatePlayer(ownerClient, "Lorenzo");

        var game = await CreateGame(ownerClient);
        await AddBuyIn(ownerClient, game.Id, player.Id, 50m);

        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);

        using var memberClient = factory.CreateHttpsClient();

        var response = await memberClient.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal(50m, gameDetails.TotalBuyInAmount);
        Assert.Equal(0m, gameDetails.TotalCashOutAmount);
        Assert.Equal(50m, gameDetails.RemainingCashOutAmount);
        var entry = Assert.Single(gameDetails.Entries);
        Assert.Equal(buyIn.Id, entry.Id);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("BuyIn", entry.Type);
        AssertCloseTo(buyIn.RecordedAtUtc, entry.RecordedAtUtc);

        var playerTotal = Assert.Single(gameDetails.PlayerTotals);
        Assert.Equal(player.Id, playerTotal.PlayerId);
        Assert.Equal("Lorenzo", playerTotal.PlayerName);
        Assert.Equal(50m, playerTotal.BuyInAmount);
        Assert.Equal(0m, playerTotal.CashOutAmount);
        Assert.Equal(-50m, playerTotal.NetAmount);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasCashOuts()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);
        var cashOut = await AddCashOut(client, game.Id, player.Id, 50m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal(75m, gameDetails.TotalBuyInAmount);
        Assert.Equal(50m, gameDetails.TotalCashOutAmount);
        Assert.Equal(25m, gameDetails.RemainingCashOutAmount);
        var entry = Assert.Single(gameDetails.Entries, entry => entry.Id == cashOut.Id);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("CashOut", entry.Type);
        AssertCloseTo(cashOut.RecordedAtUtc, entry.RecordedAtUtc);
    }

    [Fact]
    public async Task GetGame_ReturnsPlayerTotals_ForOpenGame()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");
        await AddBuyIn(client, game.Id, lorenzo.Id, 75m);
        await AddBuyIn(client, game.Id, lorenzo.Id, 25m);
        await AddBuyIn(client, game.Id, maya.Id, 50m);
        await AddCashOut(client, game.Id, lorenzo.Id, 60m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal("Open", gameDetails.Status);
        Assert.Collection(
            gameDetails.PlayerTotals,
            total =>
            {
                Assert.Equal(lorenzo.Id, total.PlayerId);
                Assert.Equal("Lorenzo", total.PlayerName);
                Assert.Equal(100m, total.BuyInAmount);
                Assert.Equal(60m, total.CashOutAmount);
                Assert.Equal(-40m, total.NetAmount);
            },
            total =>
            {
                Assert.Equal(maya.Id, total.PlayerId);
                Assert.Equal("Maya", total.PlayerName);
                Assert.Equal(50m, total.BuyInAmount);
                Assert.Equal(0m, total.CashOutAmount);
                Assert.Equal(-50m, total.NetAmount);
            });
    }

    [Fact]
    public async Task AddBuyIn_ReturnsCreatedEntry()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        Assert.NotNull(entry);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(game.Id, entry.GameId);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("BuyIn", entry.Type);
        Assert.NotEqual(default, entry.RecordedAtUtc);
        Assert.Equal($"/games/{game.Id}/entries/{entry.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{Guid.NewGuid()}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = Guid.NewGuid(), Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddBuyIn_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsCreatedEntry()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        Assert.NotNull(entry);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(game.Id, entry.GameId);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("CashOut", entry.Type);
        Assert.NotEqual(default, entry.RecordedAtUtc);
        Assert.Equal($"/games/{game.Id}/entries/{entry.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AddCashOut_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{Guid.NewGuid()}/cash-outs",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = Guid.NewGuid(), Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsConflict_WhenPlayerHasNotBoughtIn()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var playerWithBuyIn = await CreatePlayer(client, "Lorenzo");
        var playerWithoutBuyIn = await CreatePlayer(client, "Maya");
        await AddBuyIn(client, game.Id, playerWithBuyIn.Id, 100m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = playerWithoutBuyIn.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("Player must have a buy-in before cashing out.", error.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddCashOut_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsConflict_WhenCashOutsWouldExceedBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 50.01m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 1m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGameEntry_ReturnsNoContent_WhenEntryExistsInOpenGame()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.DeleteAsync($"/games/{game.Id}/entries/{buyIn.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/games/{game.Id}");
        getResponse.EnsureSuccessStatusCode();

        var gameDetails = await getResponse.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Empty(gameDetails.Entries);
        Assert.Equal(0m, gameDetails.TotalBuyInAmount);
        Assert.Equal(0m, gameDetails.RemainingCashOutAmount);
    }

    [Fact]
    public async Task DeleteGameEntry_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync($"/games/{Guid.NewGuid()}/entries/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGameEntry_ReturnsNotFound_WhenEntryDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.DeleteAsync($"/games/{game.Id}/entries/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGameEntry_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.DeleteAsync($"/games/{game.Id}/entries/{buyIn.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsUpdatedEntry_WhenEntryExistsInOpenGame()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{buyIn.Id}",
            new { Amount = 75m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        Assert.NotNull(entry);
        Assert.Equal(buyIn.Id, entry.Id);
        Assert.Equal(game.Id, entry.GameId);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(75m, entry.Amount);
        Assert.Equal("BuyIn", entry.Type);

        var getResponse = await client.GetAsync($"/games/{game.Id}");
        getResponse.EnsureSuccessStatusCode();

        var gameDetails = await getResponse.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal(75m, gameDetails.TotalBuyInAmount);
        Assert.Equal(75m, gameDetails.RemainingCashOutAmount);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"/games/{Guid.NewGuid()}/entries/{Guid.NewGuid()}",
            new { Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsNotFound_WhenEntryDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{Guid.NewGuid()}",
            new { Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task UpdateGameEntry_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{buyIn.Id}",
            new { Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{buyIn.Id}",
            new { Amount = 75m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsConflict_WhenCashOutsWouldExceedBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 100m);
        var cashOut = await AddCashOut(client, game.Id, player.Id, 75m);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{cashOut.Id}",
            new { Amount = 100.01m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGameEntry_ReturnsConflict_WhenReducedBuyInWouldMakeCashOutsExceedBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 100m);
        await AddCashOut(client, game.Id, player.Id, 75m);

        var response = await client.PutAsJsonAsync(
            $"/games/{game.Id}/entries/{buyIn.Id}",
            new { Amount = 74.99m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsOk_WhenGameIsBalanced()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var closedGame = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.NotNull(closedGame);
        Assert.Equal(game.Id, closedGame.Id);
        Assert.Equal("Closed", closedGame.Status);
        AssertCloseTo(game.CreatedAtUtc, closedGame.CreatedAtUtc);

        var getResponse = await client.GetAsync($"/games/{game.Id}");
        getResponse.EnsureSuccessStatusCode();

        var gameDetails = await getResponse.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal("Closed", gameDetails.Status);
    }

    [Fact]
    public async Task CloseGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/games/{Guid.NewGuid()}/close", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsConflict_WhenGameIsNotBalanced()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsConflict_WhenGameIsAlreadyClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GameEntryAmounts_CanBeAggregatedInDatabase()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);
        await AddBuyIn(client, game.Id, player.Id, 25m);
        await AddCashOut(client, game.Id, player.Id, 60m);
        await AddCashOut(client, game.Id, player.Id, 40m);
        await CloseGame(client, game.Id);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PokerBankDbContext>();

        var totals = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatus.Closed)
            .SelectMany(game => game.Entries.Select(entry => new
                {
                    GameId = game.Id,
                    entry.PlayerId,
                    entry.Type,
                    Amount = entry.Amount.Amount
                }))
            .GroupBy(entry => new
            {
                entry.GameId,
                entry.PlayerId
            })
            .Select(entries => new
            {
                BuyInAmount = entries.Sum(entry => entry.Type == GameEntryType.BuyIn ? entry.Amount : 0m),
                CashOutAmount = entries.Sum(entry => entry.Type == GameEntryType.CashOut ? entry.Amount : 0m)
            })
            .SingleAsync();

        Assert.Equal(100m, totals.BuyInAmount);
        Assert.Equal(100m, totals.CashOutAmount);
    }

    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Create game response was empty.");
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task<GameEntryResponse> AddBuyIn(
        HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/buy-ins",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        return entry ?? throw new InvalidOperationException("Add buy-in response was empty.");
    }

    private static async Task<GameEntryResponse> AddCashOut(
        HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/cash-outs",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        return entry ?? throw new InvalidOperationException("Add cash-out response was empty.");
    }

    private static async Task<GameResponse> CloseGame(HttpClient client, Guid gameId)
    {
        var response = await client.PostAsync($"/games/{gameId}/close", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Close game response was empty.");
    }

    private static void AssertCloseTo(DateTime expected, DateTime actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private static void AssertCloseTo(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record GameDetailsResponse(
        Guid Id,
        string Status,
        DateTime CreatedAtUtc,
        decimal TotalBuyInAmount,
        decimal TotalCashOutAmount,
        decimal RemainingCashOutAmount,
        GameEntryDetailsResponse[] Entries,
        GamePlayerTotalResponse[] PlayerTotals);

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record ErrorResponse(string Error);

    private sealed record GameEntryDetailsResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record GameEntryResponse(
        Guid Id,
        Guid GameId,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record GamePlayerTotalResponse(
        Guid PlayerId,
        string PlayerName,
        decimal BuyInAmount,
        decimal CashOutAmount,
        decimal NetAmount);
}
