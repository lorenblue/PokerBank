namespace PokerBank.Domain;

public sealed class GameEntry
{
    public GameEntry(Guid playerId, Money amount, GameEntryType type)
        : this(Guid.NewGuid(), playerId, amount, type, DateTimeOffset.UtcNow)
    {
    }

    internal GameEntry(Guid id, Guid playerId, Money amount, GameEntryType type, DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entry id is required.", nameof(id));
        }

        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(playerId));
        }

        if (!amount.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        Id = id;
        PlayerId = playerId;
        Amount = amount;
        Type = type;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; }

    public Guid PlayerId { get; }

    public Money Amount { get; }

    public GameEntryType Type { get; }

    public DateTimeOffset RecordedAtUtc { get; }
}
