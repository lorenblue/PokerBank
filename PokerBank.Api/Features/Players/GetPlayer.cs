using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class GetPlayer
{
    public static IEndpointRouteBuilder MapGetPlayer(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players/{id:guid}", Handle)
            .WithName("GetPlayer")
            .WithTags("Players")
            .WithSummary("Get a player.");

        return app;
    }

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Id == id && player.PokerGroupId == groupContext.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var now = DateTimeOffset.UtcNow;

        var pendingInvitation = await dbContext.PlayerInvitations
            .AsNoTracking()
            .Where(invitation =>
                invitation.PokerGroupId == groupContext.Id &&
                invitation.PlayerId == player.Id &&
                invitation.AcceptedAtUtc == null &&
                invitation.ExpiresAtUtc > now)
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .Select(invitation => new PendingInvitationResponse(
                invitation.Id,
                invitation.EmailAddress,
                invitation.ExpiresAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            player.Id,
            player.Name,
            player.EmailAddress,
            player.IsActive,
            player.UserId is not null,
            pendingInvitation));
    }

    private sealed record Response(
        Guid Id,
        string Name,
        string? EmailAddress,
        bool IsActive,
        bool HasUserAccount,
        PendingInvitationResponse? PendingInvitation);

    private sealed record PendingInvitationResponse(
        Guid Id,
        string EmailAddress,
        DateTimeOffset ExpiresAtUtc);
}
