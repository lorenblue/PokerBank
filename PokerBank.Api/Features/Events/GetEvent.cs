using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Me;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public static class GetEvent
{
    public static IEndpointRouteBuilder MapGetEvent(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/{id:guid}", Handle)
            .WithName("GetEvent")
            .WithTags("Events")
            .WithSummary("Get an event.");

        return app;
    }

    private static async Task<Results<Ok<EventDetailsResponse>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);
        var currentPlayerId = currentPlayer?.Id;

        var pokerEvent = await dbContext.Events
            .AsNoTracking()
            .Where(pokerEvent => pokerEvent.Id == id && pokerEvent.PokerGroupId == groupContext.Id)
            .Select(pokerEvent => new
            {
                pokerEvent.Id,
                pokerEvent.Title,
                pokerEvent.ScheduledAtUtc,
                pokerEvent.Status,
                pokerEvent.CreatedAtUtc,
                pokerEvent.CancelledAtUtc,
                GameId = dbContext.Games
                    .Where(game => game.PokerEventId == pokerEvent.Id && game.PokerGroupId == groupContext.Id)
                    .Select(game => (Guid?)game.Id)
                    .SingleOrDefault(),
                GoingCount = pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.Going),
                MaybeCount = pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.Maybe),
                NotGoingCount = pokerEvent.Rsvps.Count(rsvp => rsvp.Status == RsvpStatus.NotGoing),
                MyRsvpStatus = currentPlayerId == null
                    ? null
                    : pokerEvent.Rsvps
                        .Where(rsvp => rsvp.PlayerId == currentPlayerId)
                        .Select(rsvp => (RsvpStatus?)rsvp.Status)
                        .SingleOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (pokerEvent is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Event was not found."));
        }

        var rsvps = await dbContext.EventRsvps
            .AsNoTracking()
            .Where(rsvp => rsvp.EventId == id)
            .Join(
                dbContext.Players.AsNoTracking().Where(player => player.PokerGroupId == groupContext.Id),
                rsvp => rsvp.PlayerId,
                player => player.Id,
                (rsvp, player) => new { Rsvp = rsvp, player.Name })
            .OrderBy(rsvp => rsvp.Name)
            .Select(rsvp => new EventRsvpResponse(
                rsvp.Rsvp.PlayerId,
                rsvp.Name,
                rsvp.Rsvp.Status,
                rsvp.Rsvp.RespondedAtUtc))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new EventDetailsResponse(
            pokerEvent.Id,
            pokerEvent.Title,
            pokerEvent.ScheduledAtUtc,
            pokerEvent.Status,
            pokerEvent.CreatedAtUtc,
            pokerEvent.CancelledAtUtc,
            pokerEvent.GameId,
            pokerEvent.GoingCount,
            pokerEvent.MaybeCount,
            pokerEvent.NotGoingCount,
            pokerEvent.MyRsvpStatus,
            rsvps));
    }
}
