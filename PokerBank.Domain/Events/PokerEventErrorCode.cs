namespace PokerBank.Domain;

public enum PokerEventErrorCode
{
    InvalidTitle = 1,
    InvalidScheduledAt = 2,
    EventCancelled = 3,
    InvalidPlayerId = 4,
    InvalidRsvpStatus = 5
}
