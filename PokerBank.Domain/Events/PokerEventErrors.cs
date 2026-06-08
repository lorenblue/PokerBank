namespace PokerBank.Domain;

public static class PokerEventErrors
{
    public static PokerEventError InvalidTitle() => new(
        PokerEventErrorCode.InvalidTitle,
        "Event title is required.");

    public static PokerEventError TitleTooLong() => new(
        PokerEventErrorCode.InvalidTitle,
        $"Event title cannot exceed {PokerEvent.MaxTitleLength} characters.");

    public static PokerEventError InvalidScheduledAt() => new(
        PokerEventErrorCode.InvalidScheduledAt,
        "Event scheduled time is required.");

    public static PokerEventError EventCancelled() => new(
        PokerEventErrorCode.EventCancelled,
        "Cancelled events cannot be modified.");

    public static PokerEventError InvalidPlayerId() => new(
        PokerEventErrorCode.InvalidPlayerId,
        "Player id is required.");

    public static PokerEventError InvalidRsvpStatus() => new(
        PokerEventErrorCode.InvalidRsvpStatus,
        "RSVP status is invalid.");
}
