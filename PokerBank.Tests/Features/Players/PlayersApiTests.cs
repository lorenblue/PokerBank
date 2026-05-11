using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PokerBank.Api.Data;

namespace PokerBank.Tests.Features.Players;

public sealed class PlayersApiTests
{
    [Fact]
    public async Task CreatePlayer_ReturnsCreatedPlayer()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/players", new { Name = "Lorenzo" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("Lorenzo", player.Name);
        Assert.True(player.IsActive);
        Assert.Equal($"/players/{player.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreatePlayer_ReturnsConflict_WhenActivePlayerNameAlreadyExists()
    {
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
    public async Task ListPlayers_IncludesArchivedPlayers_WhenRequested()
    {
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RenamePlayer_UpdatesPlayerName()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.PutAsJsonAsync($"/players/{createdPlayer.Id}/name", new { Name = "Enzo" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Equal(createdPlayer.Id, player.Id);
        Assert.Equal("Enzo", player.Name);
        Assert.True(player.IsActive);
    }

    [Fact]
    public async Task RenamePlayer_ReturnsConflict_WhenActivePlayerNameAlreadyExists()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(client, "Lorenzo");
        await CreatePlayer(client, "Enzo");

        var response = await client.PutAsJsonAsync($"/players/{lorenzo.Id}/name", new { Name = "Enzo" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ArchivePlayer_MarksPlayerInactive()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var createdPlayer = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsync($"/players/{createdPlayer.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.NotNull(player);
        Assert.Equal(createdPlayer.Id, player.Id);
        Assert.False(player.IsActive);
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task ArchivePlayer(HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record ErrorResponse(string Error);

    private sealed class PokerBankApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"pokerbank-tests-{Guid.NewGuid():N}.db");

        public HttpClient CreateHttpsClient()
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<PokerBankDbContext>>();
                services.AddDbContext<PokerBankDbContext>(options =>
                    options.UseSqlite($"Data Source={_databasePath}"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            TryDelete(_databasePath);
            TryDelete($"{_databasePath}-shm");
            TryDelete($"{_databasePath}-wal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
