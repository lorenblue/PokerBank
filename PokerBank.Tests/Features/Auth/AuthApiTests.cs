using System.Net;
using System.Net.Http.Json;
using PokerBank.Api.Data;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Auth;

[Collection(ApiTestCollection.Name)]
public sealed class AuthApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        using var client = factory.CreateUnauthenticatedHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                Email = DevelopmentAuthSeed.DefaultAdminEmail,
                Password = DevelopmentAuthSeed.DefaultAdminPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("PokerBank.Auth="));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        using var client = factory.CreateUnauthenticatedHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                Email = DevelopmentAuthSeed.DefaultAdminEmail,
                Password = "wrong-password"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CurrentUser_ReturnsSignedInUser()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(user);
        Assert.Equal(DevelopmentAuthSeed.DefaultAdminEmail, user.Email);
        Assert.Equal("Owner", user.GroupRole);
    }

    [Fact]
    public async Task ProtectedWriteEndpoint_ReturnsUnauthorized_WhenUserIsNotSignedIn()
    {
        using var client = factory.CreateUnauthenticatedHttpsClient();

        var response = await client.PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record CurrentUserResponse(Guid Id, string Email, Guid PokerGroupId, string? GroupRole);
}
