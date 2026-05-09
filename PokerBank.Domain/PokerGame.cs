namespace PokerBank.Domain;

public sealed class PokerGame
{
    private readonly List<GameEntry> _entries = [];

    public PokerGame()
        : this(Guid.NewGuid())
    {
    }

    public PokerGame(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        Id = id;
        Status = GameStatus.Open;
    }

    public Guid Id { get; }

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

        if (GetPlayerNets().Values.Aggregate(Money.Zero, (total, net) => total + net) != Money.Zero)
        {
            throw new InvalidOperationException("Closed game player nets must sum to zero.");
        }
    }

    public Money GetNetForPlayer(Guid playerId)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(playerId));
        }

        var buyIns = SumEntries(GameEntryType.BuyIn, playerId);
        var cashOuts = SumEntries(GameEntryType.CashOut, playerId);

        return cashOuts - buyIns;
    }

    public IReadOnlyDictionary<Guid, Money> GetPlayerNets()
    {
        return _entries
            .Select(entry => entry.PlayerId)
            .Distinct()
            .ToDictionary(playerId => playerId, GetNetForPlayer);
    }

    private GameEntry AddEntry(Guid playerId, Money amount, GameEntryType type)
    {
        EnsureOpen();

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

    private Money SumEntries(GameEntryType type, Guid playerId)
    {
        return _entries
            .Where(entry => entry.Type == type && entry.PlayerId == playerId)
            .Select(entry => entry.Amount)
            .Aggregate(Money.Zero, (total, amount) => total + amount);
    }
}
