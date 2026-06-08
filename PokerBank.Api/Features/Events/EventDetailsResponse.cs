using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public sealed record EventDetailsResponse(
    Guid Id,
    string Title,
    DateTimeOffset ScheduledAtUtc,
    PokerEventStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    Guid? GameId,
    int GoingCount,
    int MaybeCount,
    int NotGoingCount,
    RsvpStatus? MyRsvpStatus,
    EventRsvpResponse[] Rsvps);
