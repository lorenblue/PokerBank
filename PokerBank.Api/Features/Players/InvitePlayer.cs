using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Email;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Players;

public static class InvitePlayer
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public static IEndpointRouteBuilder MapInvitePlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/{id:guid}/invite", Handle)
            .WithName("InvitePlayer")
            .WithTags("Players")
            .WithSummary("Invite a player.");

        return app;
    }

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        HttpContext httpContext,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        IEmailSender emailSender,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .SingleOrDefaultAsync(
                player => player.Id == id && player.PokerGroupId == groupContext.Id,
                cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        if (!player.IsActive)
        {
            return TypedResults.Conflict(new ErrorResponse("Archived players cannot be invited."));
        }

        if (string.IsNullOrWhiteSpace(player.EmailAddress))
        {
            return TypedResults.Conflict(new ErrorResponse("Player must have an email address before they can be invited."));
        }

        if (player.UserId is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("Player is already linked to a user account."));
        }

        var now = DateTimeOffset.UtcNow;

        var hasPendingInvitation = await dbContext.PlayerInvitations.AnyAsync(
            invitation =>
                invitation.PokerGroupId == groupContext.Id &&
                invitation.PlayerId == player.Id &&
                invitation.EmailAddress == player.EmailAddress &&
                invitation.AcceptedAtUtc == null &&
                invitation.ExpiresAtUtc > now,
            cancellationToken);

        if (hasPendingInvitation)
        {
            return TypedResults.Conflict(new ErrorResponse("Player already has a pending invitation."));
        }

        var token = PlayerInvitationToken.Create();
        var invitation = PlayerInvitation.Create(
            groupContext.Id,
            player.Id,
            player.EmailAddress,
            PlayerInvitationToken.Hash(token),
            now,
            now.Add(InvitationLifetime)).Value;

        dbContext.PlayerInvitations.Add(invitation);

        var inviteUrl = BuildInviteUrl(httpContext.Request, token);

        await emailSender.SendAsync(
            new EmailMessage(
                player.EmailAddress,
                "PokerBank invitation",
                BuildBody(player.Name, inviteUrl, invitation.ExpiresAtUtc)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            invitation.Id,
            invitation.PlayerId,
            invitation.EmailAddress,
            invitation.ExpiresAtUtc));
    }

    private static string BuildInviteUrl(HttpRequest request, string token)
    {
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;

        return $"{request.Scheme}://{request.Host}{pathBase}/accept-invite?token={Uri.EscapeDataString(token)}";
    }

    private static string BuildBody(string playerName, string inviteUrl, DateTimeOffset expiresAtUtc) =>
        $"""
        Hi {playerName},

        You have been invited to access your PokerBank balance.

        Accept your invitation:
        {inviteUrl}

        This invitation expires on {expiresAtUtc:yyyy-MM-dd}.
        """;

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        string EmailAddress,
        DateTimeOffset ExpiresAtUtc);
}
