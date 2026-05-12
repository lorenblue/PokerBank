namespace PokerBank.Domain;

public sealed class PokerGame
{
    private readonly List<GameEntry> _entries = [];

    public PokerGame()
        : this(Guid.NewGuid(), DateTime.UtcNow, GameStatus.Open)
    {
    }

    internal PokerGame(Guid id, DateTime createdAtUtc, GameStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Game status is invalid.");
        }

        Id = id;
        CreatedAtUtc = createdAtUtc;
        Status = status;
    }

    public Guid Id { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public GameStatus Status { get; private set; }

    public IReadOnlyCollection<GameEntry> Entries => _entries.AsReadOnly();

    public Money TotalBuyIns => SumEntries(GameEntryType.BuyIn);

    public Money TotalCashOuts => SumEntries(GameEntryType.CashOut);

    public GameEntry AddBuyIn(Guid playerId, Money amount) => AddEntry(playerId, amount, GameEntryType.BuyIn);

    public GameEntry AddCashOut(Guid playerId, Money amount) => AddEntry(playerId, amount, GameEntryType.CashOut);

    public void Close()
    {
        EnsureOpen();

        if (TotalBuyIns != TotalCashOuts)
        {
            throw new InvalidOperationException("Cannot close a game until total buy-ins equal total cash-outs.");
        }

        Status = GameStatus.Closed;
    }

    private GameEntry AddEntry(Guid playerId, Money amount, GameEntryType type)
    {
        EnsureOpen();

        if (type == GameEntryType.CashOut && TotalCashOuts + amount > TotalBuyIns)
        {
            throw new InvalidOperationException("Cash-outs cannot exceed total buy-ins.");
        }

        var entry = new GameEntry(playerId, amount, type);
        _entries.Add(entry);

        return entry;
    }

    private void EnsureOpen()
    {
        if (Status == GameStatus.Closed)
        {
            throw new InvalidOperationException("Closed games cannot be modified.");
        }
    }

    private Money SumEntries(GameEntryType type)
    {
        return _entries
            .Where(entry => entry.Type == type)
            .Select(entry => entry.Amount)
            .Aggregate(Money.Zero, (total, amount) => total + amount);
    }
}
