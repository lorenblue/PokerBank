using FluentResults;

namespace PokerBank.Domain;

public sealed class PokerGame
{
    private readonly List<GameEntry> _entries = [];

    private PokerGame()
    {
    }

    public static PokerGame Create() => new(Guid.NewGuid(), DateTime.UtcNow, GameStatus.Open);

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

    public Result<GameEntry> AddBuyIn(Guid playerId, Money amount) => AddEntry(playerId, amount, GameEntryType.BuyIn);

    public Result<GameEntry> AddCashOut(Guid playerId, Money amount) => AddEntry(playerId, amount, GameEntryType.CashOut);

    public Result Close()
    {
        if (IsClosed())
        {
            return Result.Fail(PokerGameErrors.GameClosed());
        }

        if (TotalBuyIns != TotalCashOuts)
        {
            return Result.Fail(PokerGameErrors.BuyInsMustEqualCashOuts());
        }

        Status = GameStatus.Closed;

        return Result.Ok();
    }

    private Result<GameEntry> AddEntry(Guid playerId, Money amount, GameEntryType type)
    {
        if (IsClosed())
        {
            return Result.Fail<GameEntry>(PokerGameErrors.GameClosed());
        }

        if (playerId == Guid.Empty)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.InvalidPlayerId());
        }

        if (!amount.IsPositive)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.InvalidAmount());
        }

        if (type == GameEntryType.CashOut && TotalCashOuts + amount > TotalBuyIns)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.CashOutsExceedBuyIns());
        }

        var entry = new GameEntry(playerId, amount, type);
        _entries.Add(entry);

        return Result.Ok(entry);
    }

    private bool IsClosed() => Status == GameStatus.Closed;

    private Money SumEntries(GameEntryType type)
    {
        return _entries
            .Where(entry => entry.Type == type)
            .Select(entry => entry.Amount)
            .Aggregate(Money.Zero, (total, amount) => total + amount);
    }
}
