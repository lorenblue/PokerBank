using FluentResults;

namespace PokerBank.Domain;

public sealed class PokerGame
{
    private readonly List<GameEntry> _entries = [];

    private PokerGame()
    {
    }

    public static PokerGame Create(Guid pokerGroupId, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), pokerGroupId, pokerEventId: null, createdAtUtc, GameStatus.Open);

    public static PokerGame CreateForEvent(Guid pokerGroupId, Guid pokerEventId, DateTimeOffset createdAtUtc)
    {
        if (pokerEventId == Guid.Empty)
        {
            throw new ArgumentException("Poker event id is required.", nameof(pokerEventId));
        }

        return new PokerGame(Guid.NewGuid(), pokerGroupId, pokerEventId, createdAtUtc, GameStatus.Open);
    }

    internal PokerGame(Guid id, Guid pokerGroupId, Guid? pokerEventId, DateTimeOffset createdAtUtc, GameStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        if (pokerGroupId == Guid.Empty)
        {
            throw new ArgumentException("Poker group id is required.", nameof(pokerGroupId));
        }

        if (pokerEventId == Guid.Empty)
        {
            throw new ArgumentException("Poker event id is required.", nameof(pokerEventId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Game status is invalid.");
        }

        Id = id;
        PokerGroupId = pokerGroupId;
        PokerEventId = pokerEventId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        Status = status;
    }

    public Guid Id { get; private set; }

    public Guid PokerGroupId { get; private set; }

    public Guid? PokerEventId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public GameStatus Status { get; private set; }

    public IReadOnlyCollection<GameEntry> Entries => _entries.AsReadOnly();

    public Money TotalBuyIns => SumEntries(GameEntryType.BuyIn);

    public Money TotalCashOuts => SumEntries(GameEntryType.CashOut);

    public Result<GameEntry> AddBuyIn(Guid playerId, Money amount, DateTimeOffset recordedAtUtc) =>
        AddEntry(playerId, amount, GameEntryType.BuyIn, recordedAtUtc);

    public Result<GameEntry> AddCashOut(Guid playerId, Money amount, DateTimeOffset recordedAtUtc) =>
        AddEntry(playerId, amount, GameEntryType.CashOut, recordedAtUtc);

    public Result RemoveEntry(Guid entryId)
    {
        if (IsClosed())
        {
            return Result.Fail(PokerGameErrors.GameClosed());
        }

        var entry = _entries.SingleOrDefault(entry => entry.Id == entryId);

        if (entry is null)
        {
            return Result.Fail(PokerGameErrors.EntryNotFound());
        }

        _entries.Remove(entry);

        return Result.Ok();
    }

    public Result<GameEntry> UpdateEntryAmount(Guid entryId, Money amount)
    {
        if (IsClosed())
        {
            return Result.Fail<GameEntry>(PokerGameErrors.GameClosed());
        }

        if (!amount.IsPositive)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.InvalidAmount());
        }

        var entry = _entries.SingleOrDefault(entry => entry.Id == entryId);

        if (entry is null)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.EntryNotFound());
        }

        var updatedBuyIns = TotalBuyIns;
        var updatedCashOuts = TotalCashOuts;

        if (entry.Type == GameEntryType.BuyIn)
        {
            updatedBuyIns = updatedBuyIns - entry.Amount + amount;
        }
        else
        {
            updatedCashOuts = updatedCashOuts - entry.Amount + amount;
        }

        if (updatedCashOuts > updatedBuyIns)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.CashOutsExceedBuyIns());
        }

        entry.UpdateAmount(amount);

        return Result.Ok(entry);
    }

    public Result Close()
    {
        if (IsClosed())
        {
            return Result.Fail(PokerGameErrors.GameClosed());
        }

        if (_entries.Count == 0)
        {
            return Result.Fail(PokerGameErrors.EmptyGame());
        }

        if (TotalBuyIns != TotalCashOuts)
        {
            return Result.Fail(PokerGameErrors.BuyInsMustEqualCashOuts());
        }

        Status = GameStatus.Closed;

        return Result.Ok();
    }

    private Result<GameEntry> AddEntry(Guid playerId, Money amount, GameEntryType type, DateTimeOffset recordedAtUtc)
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

        if (recordedAtUtc.ToUniversalTime() < CreatedAtUtc)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.EntryRecordedBeforeGameCreated());
        }

        if (type == GameEntryType.CashOut && !HasBuyIn(playerId))
        {
            return Result.Fail<GameEntry>(PokerGameErrors.PlayerHasNoBuyIns());
        }

        if (type == GameEntryType.CashOut && TotalCashOuts + amount > TotalBuyIns)
        {
            return Result.Fail<GameEntry>(PokerGameErrors.CashOutsExceedBuyIns());
        }

        var entry = new GameEntry(playerId, amount, type, recordedAtUtc);
        _entries.Add(entry);

        return Result.Ok(entry);
    }

    private bool IsClosed() => Status == GameStatus.Closed;

    private bool HasBuyIn(Guid playerId)
    {
        return _entries.Any(entry => entry.PlayerId == playerId && entry.Type == GameEntryType.BuyIn);
    }

    private Money SumEntries(GameEntryType type)
    {
        return _entries
            .Where(entry => entry.Type == type)
            .Select(entry => entry.Amount)
            .Aggregate(Money.Zero, (total, amount) => total + amount);
    }
}
