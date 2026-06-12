using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Events;

public static class CancelEvent
{
    public static IEndpointRouteBuilder MapCancelEvent(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/cancel", Handle)
            .WithName("CancelEvent")
            .WithTags("Events")
            .WithSummary("Cancel an event.");

        return app;
    }

    private static async Task<Results<Ok<EventResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var pokerEvent = await dbContext.Events
            .Include(pokerEvent => pokerEvent.Rsvps)
            .SingleOrDefaultAsync(
                pokerEvent => pokerEvent.Id == id && pokerEvent.PokerGroupId == groupContext.Id,
                cancellationToken);

        if (pokerEvent is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Event was not found."));
        }

        var linkedGameId = await dbContext.Games
            .Where(game => game.PokerEventId == pokerEvent.Id && game.PokerGroupId == groupContext.Id)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (linkedGameId is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("Events with linked games cannot be cancelled."));
        }

        var result = pokerEvent.Cancel(timeProvider.GetUtcNow());

        if (result.IsFailed)
        {
            return TypedResults.Conflict(EventResultMapper.ToError(result));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new EventResponse(
            pokerEvent.Id,
            pokerEvent.Title,
            pokerEvent.ScheduledAtUtc,
            pokerEvent.Status,
            pokerEvent.CreatedAtUtc,
            pokerEvent.CancelledAtUtc,
            GameId: null,
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.Going),
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.Maybe),
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.NotGoing),
            MyRsvpStatus: null));
    }
}
