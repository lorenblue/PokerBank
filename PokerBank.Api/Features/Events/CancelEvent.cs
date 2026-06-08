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

        var result = pokerEvent.Cancel(DateTimeOffset.UtcNow);

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
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.Going),
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.Maybe),
            pokerEvent.Rsvps.Count(rsvp => rsvp.Status == Domain.RsvpStatus.NotGoing),
            MyRsvpStatus: null));
    }
}
