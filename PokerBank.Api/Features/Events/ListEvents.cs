using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Me;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public static class ListEvents
{
    public static IEndpointRouteBuilder MapListEvents(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events", Handle)
            .WithName("ListEvents")
            .WithTags("Events")
            .WithSummary("List events.");

        return app;
    }

    private static async Task<Ok<EventResponse[]>> Handle(
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);
        var currentPlayerId = currentPlayer?.Id;

        var events = await dbContext.Events
            .AsNoTracking()
            .Where(pokerEvent => pokerEvent.PokerGroupId == groupContext.Id)
            .OrderBy(pokerEvent => pokerEvent.ScheduledAtUtc)
            .Select(pokerEvent => new EventResponse(
                pokerEvent.Id,
                pokerEvent.Title,
                pokerEvent.ScheduledAtUtc,
                pokerEvent.Status,
                pokerEvent.CreatedAtUtc,
                pokerEvent.CancelledAtUtc,
                pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.Going),
                pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.Maybe),
                pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.NotGoing),
                currentPlayerId == null
                    ? null
                    : pokerEvent.Rsvps
                        .Where(rsvp => rsvp.PlayerId == currentPlayerId)
                        .Select(rsvp => (RsvpStatus?)rsvp.Status)
                        .SingleOrDefault()))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(events);
    }
}
