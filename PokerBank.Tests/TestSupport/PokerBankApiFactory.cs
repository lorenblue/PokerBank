using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PokerBank.Api.Data;
using PokerBank.Api.Email;
using PokerBank.Domain;
using Testcontainers.PostgreSql;

namespace PokerBank.Tests.TestSupport;

public sealed class PokerBankApiFactory : WebApplicationFactory<Program>
{
    private readonly RecordingEmailSender _emailSender = new();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pokerbank")
        .WithUsername("pokerbank")
        .WithPassword("pokerbank")
        .Build();

    public PokerBankApiFactory()
    {
        _postgres.StartAsync().GetAwaiter().GetResult();
        using var client = CreateUnauthenticatedHttpsClient();
    }

    public HttpClient CreateHttpsClient()
    {
        var client = CreateUnauthenticatedHttpsClient();
        SignInAsync(client).GetAwaiter().GetResult();

        return client;
    }

    public HttpClient CreateUnauthenticatedHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public IReadOnlyCollection<EmailMessage> SentEmails => _emailSender.Messages;

    public HttpClient CreateHttpsClient(Guid pokerGroupId)
    {
        var client = WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPokerGroupContext>();
                services.AddScoped<IPokerGroupContext>(_ => new TestPokerGroupContext(pokerGroupId));
            }))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        SignInAsync(client).GetAwaiter().GetResult();

        return client;
    }

    public async Task CreatePokerGroupAsync(Guid id, string name = "Other Group")
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "PokerGroups" ("Id", "Name", "IsActive")
            VALUES (@id, @name, TRUE)
            ON CONFLICT ("Id") DO NOTHING;

            INSERT INTO "GroupMemberships" ("UserId", "PokerGroupId", "Role")
            SELECT "Id", @id, @role
            FROM "AspNetUsers"
            WHERE "NormalizedEmail" = @normalizedEmail
            ON CONFLICT ("UserId", "PokerGroupId") DO NOTHING;
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("role", GroupRole.Owner.ToString());
        command.Parameters.AddWithValue("normalizedEmail", DevelopmentAuthSeed.DefaultAdminEmail.ToUpperInvariant());

        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            TRUNCATE TABLE "GameEntries", "Payments", "Games", "Players" RESTART IDENTITY CASCADE;

            UPDATE "GroupMemberships"
            SET "Role" = @ownerRole
            WHERE "PokerGroupId" = @defaultPokerGroupId
              AND "UserId" IN (
                  SELECT "Id"
                  FROM "AspNetUsers"
                  WHERE "NormalizedEmail" = @normalizedEmail
              );
            """;
        command.Parameters.AddWithValue("ownerRole", GroupRole.Owner.ToString());
        command.Parameters.AddWithValue("defaultPokerGroupId", DefaultPokerGroup.Id);
        command.Parameters.AddWithValue("normalizedEmail", DevelopmentAuthSeed.DefaultAdminEmail.ToUpperInvariant());

        await command.ExecuteNonQueryAsync();
        _emailSender.Clear();
    }

    public async Task SetDefaultAdminRoleAsync(GroupRole role)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE "GroupMemberships"
            SET "Role" = @role
            WHERE "PokerGroupId" = @defaultPokerGroupId
              AND "UserId" IN (
                  SELECT "Id"
                  FROM "AspNetUsers"
                  WHERE "NormalizedEmail" = @normalizedEmail
              );
            """;
        command.Parameters.AddWithValue("role", role.ToString());
        command.Parameters.AddWithValue("defaultPokerGroupId", DefaultPokerGroup.Id);
        command.Parameters.AddWithValue("normalizedEmail", DevelopmentAuthSeed.DefaultAdminEmail.ToUpperInvariant());

        await command.ExecuteNonQueryAsync();
    }

    public async Task LinkDefaultAdminToPlayerAsync(Guid playerId)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE "Players"
            SET "UserId" = (
                SELECT "Id"
                FROM "AspNetUsers"
                WHERE "NormalizedEmail" = @normalizedEmail
            )
            WHERE "Id" = @playerId;
            """;
        command.Parameters.AddWithValue("normalizedEmail", DevelopmentAuthSeed.DefaultAdminEmail.ToUpperInvariant());
        command.Parameters.AddWithValue("playerId", playerId);

        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PokerBank", _postgres.GetConnectionString());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_emailSender);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class TestPokerGroupContext(Guid id) : IPokerGroupContext
    {
        public Guid Id { get; } = id;
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        private readonly List<EmailMessage> _messages = [];
        private readonly Lock _lock = new();

        public IReadOnlyCollection<EmailMessage> Messages
        {
            get
            {
                lock (_lock)
                {
                    return _messages.ToArray();
                }
            }
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _messages.Add(message);
            }

            return Task.CompletedTask;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }
    }

    private static async Task SignInAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                Email = DevelopmentAuthSeed.DefaultAdminEmail,
                Password = DevelopmentAuthSeed.DefaultAdminPassword
            });

        response.EnsureSuccessStatusCode();
    }
}
