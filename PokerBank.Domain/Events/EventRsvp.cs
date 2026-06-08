namespace PokerBank.Domain;

public sealed class EventRsvp
{
    private EventRsvp()
    {
    }

    internal EventRsvp(Guid eventId, Guid playerId, RsvpStatus status, DateTimeOffset respondedAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.", nameof(eventId));
        }

        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(playerId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "RSVP status is invalid.");
        }

        EventId = eventId;
        PlayerId = playerId;
        Status = status;
        RespondedAtUtc = respondedAtUtc.ToUniversalTime();
    }

    public Guid EventId { get; private set; }

    public Guid PlayerId { get; private set; }

    public RsvpStatus Status { get; private set; }

    public DateTimeOffset RespondedAtUtc { get; private set; }

    internal void Update(RsvpStatus status, DateTimeOffset respondedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "RSVP status is invalid.");
        }

        Status = status;
        RespondedAtUtc = respondedAtUtc.ToUniversalTime();
    }
}
