using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Payments;

[Collection(ApiTestCollection.Name)]
public sealed class PaymentsApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("PlayerPaysBank")]
    [InlineData("BankPaysPlayer")]
    public async Task CreatePayment_ReturnsCreatedPayment(string type)
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = player.Id, Amount = 40m, Type = type, Method = "ETransfer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(player.Id, payment.PlayerId);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal(type, payment.Type);
        Assert.Equal("ETransfer", payment.Method);
        Assert.NotEqual(default, payment.RecordedAtUtc);
        Assert.Equal($"/payments/{payment.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetPayment_ReturnsPayment_WhenPaymentExists()
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");
        var createdPayment = await CreatePayment(client, player.Id, 40m, "PlayerPaysBank");

        var response = await client.GetAsync($"/payments/{createdPayment.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(payment);
        Assert.Equal(createdPayment.Id, payment.Id);
        Assert.Equal(player.Id, payment.PlayerId);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal("PlayerPaysBank", payment.Type);
        Assert.Equal("ETransfer", payment.Method);
        AssertCloseTo(createdPayment.RecordedAtUtc, payment.RecordedAtUtc);
    }

    [Fact]
    public async Task GetPayment_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListPayments_ReturnsPaymentsNewestFirst()
    {
        using var client = factory.CreateHttpsClient();
        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var olderPayment = await CreatePayment(client, lorenzo.Id, 40m, "PlayerPaysBank");
        await Task.Delay(10);
        var newerPayment = await CreatePayment(client, maya.Id, 25m, "BankPaysPlayer");

        var response = await client.GetAsync("/payments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payments = await response.Content.ReadFromJsonAsync<PaymentResponse[]>();

        Assert.NotNull(payments);
        Assert.Collection(
            payments,
            payment =>
            {
                Assert.Equal(newerPayment.Id, payment.Id);
                Assert.Equal(maya.Id, payment.PlayerId);
                Assert.Equal(25m, payment.Amount);
                Assert.Equal("BankPaysPlayer", payment.Type);
                Assert.Equal("ETransfer", payment.Method);
            },
            payment =>
            {
                Assert.Equal(olderPayment.Id, payment.Id);
                Assert.Equal(lorenzo.Id, payment.PlayerId);
                Assert.Equal(40m, payment.Amount);
                Assert.Equal("PlayerPaysBank", payment.Type);
                Assert.Equal("ETransfer", payment.Method);
            });
    }

    [Fact]
    public async Task ListPayments_FiltersByPlayer()
    {
        using var client = factory.CreateHttpsClient();
        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var lorenzoPayment = await CreatePayment(client, lorenzo.Id, 40m, "PlayerPaysBank");
        await CreatePayment(client, maya.Id, 25m, "BankPaysPlayer");

        var response = await client.GetAsync($"/payments?playerId={lorenzo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payments = await response.Content.ReadFromJsonAsync<PaymentResponse[]>();

        Assert.NotNull(payments);
        var payment = Assert.Single(payments);
        Assert.Equal(lorenzoPayment.Id, payment.Id);
        Assert.Equal(lorenzo.Id, payment.PlayerId);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal("PlayerPaysBank", payment.Type);
        Assert.Equal("ETransfer", payment.Method);
    }

    [Fact]
    public async Task DeletePayment_ReturnsNoContent_WhenPaymentExists()
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");
        var payment = await CreatePayment(client, player.Id, 40m, "PlayerPaysBank");

        var response = await client.DeleteAsync($"/payments/{payment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/payments/{payment.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePayment_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.DeleteAsync($"/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePayment_UpdatesBalances()
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");
        var payment = await CreatePayment(client, player.Id, 40m, "PlayerPaysBank");

        var deleteResponse = await client.DeleteAsync($"/payments/{payment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var balanceResponse = await client.GetAsync($"/balances?playerId={player.Id}");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);

        var balances = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        var balance = Assert.Single(balances);
        Assert.Equal(0m, balance.PaymentNetAmount);
        Assert.Equal(0m, balance.BalanceAmount);
    }

    [Fact]
    public async Task CreatePayment_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = Guid.NewGuid(), Amount = 40m, Type = "PlayerPaysBank", Method = "ETransfer" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_ReturnsNotFound_WhenPlayerIsArchived()
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");
        await ArchivePlayer(client, player.Id);

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = player.Id, Amount = 40m, Type = "PlayerPaysBank", Method = "ETransfer" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CreatePayment_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = player.Id, Amount = amount, Type = "PlayerPaysBank", Method = "ETransfer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nope")]
    public async Task CreatePayment_ReturnsBadRequest_WhenPaymentTypeIsInvalid(string? type)
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = player.Id, Amount = 40m, Type = type, Method = "ETransfer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nope")]
    public async Task CreatePayment_ReturnsBadRequest_WhenPaymentMethodIsInvalid(string? method)
    {
        using var client = factory.CreateHttpsClient();
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = player.Id, Amount = 40m, Type = "PlayerPaysBank", Method = method });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task<PaymentResponse> CreatePayment(
        HttpClient client,
        Guid playerId,
        decimal amount,
        string type)
    {
        var response = await client.PostAsJsonAsync(
            "/payments",
            new { PlayerId = playerId, Amount = amount, Type = type, Method = "ETransfer" });
        response.EnsureSuccessStatusCode();

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        return payment ?? throw new InvalidOperationException("Create payment response was empty.");
    }

    private static async Task ArchivePlayer(HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static void AssertCloseTo(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record PaymentResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        string Method,
        DateTimeOffset RecordedAtUtc);

    private sealed record BalanceResponse(
        Guid PlayerId,
        string PlayerName,
        bool IsActive,
        decimal GameNetAmount,
        decimal PaymentNetAmount,
        decimal BalanceAmount);
}
