namespace PokerBank.Domain;

public sealed class GameEntry
{
    private GameEntry()
    {
    }

    internal GameEntry(Guid playerId, Money amount, GameEntryType type)
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

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Game entry type is invalid.");
        }

        Id = id;
        PlayerId = playerId;
        Amount = amount;
        Type = type;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PlayerId { get; private set; }

    public Money Amount { get; private set; }

    public GameEntryType Type { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    internal void UpdateAmount(Money amount)
    {
        if (!amount.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        Amount = amount;
    }
}
