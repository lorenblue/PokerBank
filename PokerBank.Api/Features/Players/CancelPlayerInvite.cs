using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class CancelPlayerInvite
{
    public static IEndpointRouteBuilder MapCancelPlayerInvite(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/players/{id:guid}/invite", Handle)
            .WithName("CancelPlayerInvite")
            .WithTags("Players")
            .WithSummary("Cancel a pending player invite.");

        return app;
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players.AnyAsync(
            player => player.Id == id && player.PokerGroupId == groupContext.Id,
            cancellationToken);

        if (!playerExists)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var now = DateTimeOffset.UtcNow;

        var invitation = await dbContext.PlayerInvitations
            .Where(invitation =>
                invitation.PokerGroupId == groupContext.Id &&
                invitation.PlayerId == id &&
                invitation.AcceptedAtUtc == null &&
                invitation.ExpiresAtUtc > now)
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Pending invitation was not found."));
        }

        dbContext.PlayerInvitations.Remove(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
