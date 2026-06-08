using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public sealed record EventRsvpResponse(
    Guid PlayerId,
    string PlayerName,
    RsvpStatus Status,
    DateTimeOffset RespondedAtUtc);
