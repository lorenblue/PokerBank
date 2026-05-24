using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Players;

[Collection(ApiTestCollection.Name)]
public sealed class PlayersApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreatePlayer_ReturnsCreatedPlayer()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/players",
            new { Name = "Lorenzo", EmailAddress = "lorenzo@example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("Lorenzo", player.Name);
        Assert.Equal("lorenzo@example.com", player.EmailAddress);
        Assert.True(player.IsActive);
        Assert.Equal($"/players/{player.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreatePlayer_ReturnsConflict_WhenActivePlayerNameAlreadyExists()
    {
        using var client = factory.CreateHttpsClient();

        await client.PostAsJsonAsync("/players", new { Name = "Lorenzo" });

        var response = await client.PostAsJsonAsync("/players", new { Name = "Lorenzo" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("An active player with this name already exists.", error?.Error);
    }

    [Fact]
    public async Task ListPlayers_ReturnsActivePlayersOrderedByName()
    {
        using var client = factory.CreateHttpsClient();

        var charlie = await CreatePlayer(client, "Charlie");
        var alice = await CreatePlayer(client, "Alice");
        await ArchivePlayer(client, charlie.Id);

        var response = await client.GetAsync("/players");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var players = await response.Content.ReadFromJsonAsync<PlayerResponse[]>();

        Assert.NotNull(players);
        var player = Assert.Single(players);
        Assert.Equal(alice.Id, player.Id);
        Assert.Equal("Alice", player.Name);
        Assert.True(player.IsActive);
    }

    [Fact]
    public async Task ListPlayers_ReturnsOnlyPlayersInCurrentPokerGroup()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        var currentGroupPlayer = await CreatePlayer(client, "Lorenzo");
        await CreatePlayer(otherGroupClient, "Maya");

        var response = await client.GetAsync("/players");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var players = await response.Content.ReadFromJsonAsync<PlayerResponse[]>();

        Assert.NotNull(players);
        var player = Assert.Single(players);
        Assert.Equal(currentGroupPlayer.Id, player.Id);
        Assert.Equal("Lorenzo", player.Name);
    }

    [Fact]
    public async Task CreatePlayer_AllowsSameNameInDifferentPokerGroups()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        await CreatePlayer(otherGroupClient, "Lorenzo");

        var response = await client.PostAsJsonAsync("/players", new { Name = "Lorenzo" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ListPlayers_IncludesArchivedPlayers_WhenRequested()
    {
        using var client = factory.CreateHttpsClient();

        var archivedPlayer = await CreatePlayer(client, "Charlie");
        var activePlayer = await CreatePlayer(client, "Alice");
        await ArchivePlayer(client, archivedPlayer.Id);

        var response = await client.GetAsync("/players?includeArchived=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var players = await response.Content.ReadFromJsonAsync<PlayerResponse[]>();

        Assert.NotNull(players);
        Assert.Collection(
            players,
            player =>
            {
                Assert.Equal(activePlayer.Id, player.Id);
                Assert.True(player.IsActive);
            },
            player =>
            {
                Assert.Equal(archivedPlayer.Id, player.Id);
                Assert.False(player.IsActive);
            });
    }

    [Fact]
    public async Task GetPlayer_ReturnsPlayer_WhenPlayerExists()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.GetAsync($"/players/{createdPlayer.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.Equal(createdPlayer, player);
    }

    [Fact]
    public async Task GetPlayer_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayer_UpdatesPlayerDetails()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.PutAsJsonAsync(
            $"/players/{createdPlayer.Id}",
            new { Name = "Enzo", EmailAddress = "enzo@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Equal(createdPlayer.Id, player.Id);
        Assert.Equal("Enzo", player.Name);
        Assert.Equal("enzo@example.com", player.EmailAddress);
        Assert.True(player.IsActive);
    }

    [Fact]
    public async Task UpdatePlayer_ClearsPlayerEmailAddress()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo", "lorenzo@example.com");

        var response = await client.PutAsJsonAsync(
            $"/players/{createdPlayer.Id}",
            new { Name = "Lorenzo", EmailAddress = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Null(player.EmailAddress);
    }

    [Fact]
    public async Task UpdatePlayer_ReturnsBadRequest_WhenEmailAddressIsInvalid()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.PutAsJsonAsync(
            $"/players/{createdPlayer.Id}",
            new { Name = "Lorenzo", EmailAddress = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("Player email address is invalid.", error?.Error);
    }

    [Fact]
    public async Task UpdatePlayer_ReturnsConflict_WhenActivePlayerNameAlreadyExists()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(client, "Lorenzo");
        await CreatePlayer(client, "Enzo");

        var response = await client.PutAsJsonAsync(
            $"/players/{lorenzo.Id}",
            new { Name = "Enzo", EmailAddress = "lorenzo@example.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ArchivePlayer_MarksPlayerInactive()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsync($"/players/{createdPlayer.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Equal(createdPlayer.Id, player.Id);
        Assert.False(player.IsActive);
    }

    private static async Task<PlayerResponse> CreatePlayer(
        HttpClient client,
        string name,
        string? emailAddress = null)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name, EmailAddress = emailAddress });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task ArchivePlayer(HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record PlayerResponse(Guid Id, string Name, string? EmailAddress, bool IsActive);

    private sealed record ErrorResponse(string Error);
}
