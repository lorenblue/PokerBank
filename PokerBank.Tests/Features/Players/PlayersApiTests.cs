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
    public async Task CreatePlayer_ReturnsConflict_WhenActivePlayerEmailAlreadyExists()
    {
        using var client = factory.CreateHttpsClient();

        await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

        var response = await client.PostAsJsonAsync(
            "/players",
            new { Name = "Enzo", EmailAddress = "Lorenzo@Example.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("An active player with this email address already exists.", error?.Error);
    }

    [Fact]
    public async Task ListPlayers_ReturnsActivePlayersOrderedByName()
    {
        using var client = factory.CreateHttpsClient();

        var charlie = await client.CreatePlayerAsync("Charlie");
        var alice = await client.CreatePlayerAsync("Alice");
        await client.ArchivePlayerAsync(charlie.Id);

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

        var currentGroupPlayer = await client.CreatePlayerAsync("Lorenzo");
        await otherGroupClient.CreatePlayerAsync("Maya");

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

        await otherGroupClient.CreatePlayerAsync("Lorenzo");

        var response = await client.PostAsJsonAsync("/players", new { Name = "Lorenzo" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayer_AllowsSameEmailInDifferentPokerGroups()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        await otherGroupClient.CreatePlayerAsync("Maya", "lorenzo@example.com");

        var response = await client.PostAsJsonAsync(
            "/players",
            new { Name = "Lorenzo", EmailAddress = "lorenzo@example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayer_AllowsSameEmailAsArchivedPlayer()
    {
        using var client = factory.CreateHttpsClient();

        var archivedPlayer = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await client.ArchivePlayerAsync(archivedPlayer.Id);

        var response = await client.PostAsJsonAsync(
            "/players",
            new { Name = "Enzo", EmailAddress = "lorenzo@example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ListPlayers_IncludesArchivedPlayers_WhenRequested()
    {
        using var client = factory.CreateHttpsClient();

        var archivedPlayer = await client.CreatePlayerAsync("Charlie");
        var activePlayer = await client.CreatePlayerAsync("Alice");
        await client.ArchivePlayerAsync(archivedPlayer.Id);

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

        var createdPlayer = await client.CreatePlayerAsync("Lorenzo");

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

        var createdPlayer = await client.CreatePlayerAsync("Lorenzo");

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

        var createdPlayer = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

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

        var createdPlayer = await client.CreatePlayerAsync("Lorenzo");

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

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        await client.CreatePlayerAsync("Enzo");

        var response = await client.PutAsJsonAsync(
            $"/players/{lorenzo.Id}",
            new { Name = "Enzo", EmailAddress = "lorenzo@example.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayer_ReturnsConflict_WhenActivePlayerEmailAlreadyExists()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await client.CreatePlayerAsync("Enzo", "enzo@example.com");

        var response = await client.PutAsJsonAsync(
            $"/players/{lorenzo.Id}",
            new { Name = "Lorenzo", EmailAddress = "Enzo@Example.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("An active player with this email address already exists.", error?.Error);
    }

    [Fact]
    public async Task ArchivePlayer_MarksPlayerInactive()
    {
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await client.CreatePlayerAsync("Lorenzo");

        var response = await client.PostAsync($"/players/{createdPlayer.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Equal(createdPlayer.Id, player.Id);
        Assert.False(player.IsActive);
    }

    [Fact]
    public async Task InvitePlayer_SendsInviteEmailAndCreatesInvitation()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

        var response = await client.PostAsync($"/players/{player.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invitation = await response.Content.ReadFromJsonAsync<InvitePlayerResponse>();

        Assert.NotNull(invitation);
        Assert.NotEqual(Guid.Empty, invitation.Id);
        Assert.Equal(player.Id, invitation.PlayerId);
        Assert.Equal("lorenzo@example.com", invitation.EmailAddress);
        Assert.True(invitation.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.Equal(1, await factory.CountPlayerInvitationsAsync(player.Id));

        var email = Assert.Single(factory.SentEmails);
        Assert.Equal("lorenzo@example.com", email.To);
        Assert.Equal("PokerBank invitation", email.Subject);
        Assert.Contains("Hi Lorenzo", email.Body);
        Assert.Contains("http://localhost:5173/accept-invite?token=", email.Body);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/players/{Guid.NewGuid()}/invite", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsNotFound_WhenPlayerIsInDifferentPokerGroup()
    {
        var otherGroupId = Guid.NewGuid();
        await factory.CreatePokerGroupAsync(otherGroupId);

        using var client = factory.CreateHttpsClient();
        using var otherGroupClient = factory.CreateHttpsClient(otherGroupId);

        var otherGroupPlayer = await otherGroupClient.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

        var response = await client.PostAsync($"/players/{otherGroupPlayer.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsConflict_WhenPlayerIsArchived()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await client.ArchivePlayerAsync(player.Id);

        var response = await client.PostAsync($"/players/{player.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("Archived players cannot be invited.", error?.Error);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsConflict_WhenPlayerHasNoEmailAddress()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo");

        var response = await client.PostAsync($"/players/{player.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("Player must have an email address before they can be invited.", error?.Error);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsConflict_WhenPlayerIsAlreadyLinkedToUser()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await factory.LinkDefaultAdminToPlayerAsync(player.Id);

        var response = await client.PostAsync($"/players/{player.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("Player is already linked to a user account.", error?.Error);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task InvitePlayer_ReturnsConflict_WhenPlayerAlreadyHasPendingInvitation()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");

        await client.InvitePlayerAsync(player.Id);

        var secondResponse = await client.PostAsync($"/players/{player.Id}/invite", content: null);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var error = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal("Player already has a pending invitation.", error?.Error);
        Assert.Equal(1, await factory.CountPlayerInvitationsAsync(player.Id));
        Assert.Single(factory.SentEmails);
    }

}
