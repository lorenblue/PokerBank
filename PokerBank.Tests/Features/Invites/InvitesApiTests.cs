using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Invites;

[Collection(ApiTestCollection.Name)]
public sealed class InvitesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcceptInvitation_CreatesUserLinksPlayerAndSignsIn()
    {
        using var managerClient = factory.CreateHttpsClient();
        using var inviteeClient = factory.CreateUnauthenticatedHttpsClient();

        var player = await managerClient.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await managerClient.InvitePlayerAsync(player.Id);

        var email = Assert.Single(factory.SentEmails);
        var token = GetInviteToken(email.Body);

        var response = await inviteeClient.PostAsJsonAsync("/invites/accept", new
        {
            Token = token,
            Password = "Password123!",
        });

        Assert.True(response.IsSuccessStatusCode);

        response = await inviteeClient.GetAsync("/auth/me");

        Assert.True(response.IsSuccessStatusCode);

        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(currentUser);
        Assert.Equal("lorenzo@example.com", currentUser.Email);
        Assert.Equal("Member", currentUser.GroupRole);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsBadRequest_WhenPasswordFailsIdentityRules()
    {
        using var managerClient = factory.CreateHttpsClient();
        using var inviteeClient = factory.CreateUnauthenticatedHttpsClient();

        var player = await managerClient.CreatePlayerAsync("Lorenzo", "lorenzo@example.com");
        await managerClient.InvitePlayerAsync(player.Id);

        var email = Assert.Single(factory.SentEmails);
        var token = GetInviteToken(email.Body);

        var response = await inviteeClient.PostAsJsonAsync("/invites/accept", new
        {
            Token = token,
            Password = "password123!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Contains("uppercase", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetInviteToken(string body)
    {
        const string marker = "token=";

        var tokenStart = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(tokenStart >= 0, "Invite email did not contain a token.");

        tokenStart += marker.Length;

        var tokenEnd = body.IndexOfAny(['\r', '\n'], tokenStart);

        return tokenEnd < 0
            ? body[tokenStart..].Trim()
            : body[tokenStart..tokenEnd].Trim();
    }

    private sealed record CurrentUserResponse(Guid Id, string Email, Guid PokerGroupId, string? GroupRole);
}
