using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PokerBank.Api.Data;
using Testcontainers.PostgreSql;

namespace PokerBank.Tests.TestSupport;

public sealed class PokerBankApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pokerbank")
        .WithUsername("pokerbank")
        .WithPassword("pokerbank")
        .Build();

    public PokerBankApiFactory()
    {
        _postgres.StartAsync().GetAwaiter().GetResult();
        using var client = CreateHttpsClient();
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public HttpClient CreateHttpsClient(Guid pokerGroupId)
    {
        return WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICurrentPokerGroup>();
                services.AddScoped<ICurrentPokerGroup>(_ => new TestCurrentPokerGroup(pokerGroupId));
            }))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
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
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            TRUNCATE TABLE "GameEntries", "Payments", "Games", "Players" RESTART IDENTITY CASCADE;
            """;

        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PokerBank", _postgres.GetConnectionString());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class TestCurrentPokerGroup(Guid id) : ICurrentPokerGroup
    {
        public Guid Id { get; } = id;
    }
}
