using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Events;

public static class UpdateEvent
{
    public static IEndpointRouteBuilder MapUpdateEvent(this IEndpointRouteBuilder app)
    {
        app.MapPut("/events/{id:guid}", Handle)
            .WithName("UpdateEvent")
            .WithTags("Events")
            .WithSummary("Update an event.");

        return app;
    }

    private static async Task<Results<Ok<EventResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        Request request,
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

        var linkedGameId = await dbContext.Games
            .Where(game => game.PokerEventId == pokerEvent.Id && game.PokerGroupId == groupContext.Id)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (linkedGameId is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("Events with linked games cannot be updated."));
        }

        var result = pokerEvent.UpdateDetails(request.Title, request.ScheduledAtUtc);

        if (result.IsFailed)
        {
            var error = EventResultMapper.ToError(result);

            return EventResultMapper.IsConflict(result)
                ? TypedResults.Conflict(error)
                : TypedResults.BadRequest(error);
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

    private sealed record Request(string? Title, DateTimeOffset ScheduledAtUtc);
}
