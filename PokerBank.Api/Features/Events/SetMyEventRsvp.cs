using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Me;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public static class SetMyEventRsvp
{
    public static IEndpointRouteBuilder MapSetMyEventRsvp(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/rsvp", Handle)
            .WithName("SetMyEventRsvp")
            .WithTags("Events")
            .WithSummary("Set my event RSVP.");

        return app;
    }

    private static async Task<Results<Ok<EventRsvpResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        Request request,
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);

        if (currentPlayer is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player profile was not found."));
        }

        var pokerEvent = await dbContext.Events
            .Include(pokerEvent => pokerEvent.Rsvps)
            .SingleOrDefaultAsync(
                pokerEvent => pokerEvent.Id == id && pokerEvent.PokerGroupId == groupContext.Id,
                cancellationToken);

        if (pokerEvent is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Event was not found."));
        }

        var result = pokerEvent.SetRsvp(currentPlayer.Id, request.Status, timeProvider.GetUtcNow());

        if (result.IsFailed)
        {
            var error = EventResultMapper.ToError(result);

            return EventResultMapper.IsConflict(result)
                ? TypedResults.Conflict(error)
                : TypedResults.BadRequest(error);
        }

        var playerName = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Id == currentPlayer.Id && player.PokerGroupId == groupContext.Id)
            .Select(player => player.Name)
            .SingleAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var rsvp = result.Value;

        return TypedResults.Ok(new EventRsvpResponse(
            rsvp.PlayerId,
            playerName,
            rsvp.Status,
            rsvp.RespondedAtUtc));
    }

    private sealed record Request(RsvpStatus Status);
}
