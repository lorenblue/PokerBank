using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PokerGameTests
{
    private static readonly Guid PokerGroupId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RecordedAtUtc = new(2026, 6, 11, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StartsOpen()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Create_RecordsGameDetails()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(PokerGroupId, game.PokerGroupId);
        Assert.Null(game.PokerEventId);
        Assert.Equal(CreatedAtUtc, game.CreatedAtUtc);
    }

    [Fact]
    public void CreateForEvent_RecordsEventLink()
    {
        var pokerEventId = Guid.NewGuid();

        var game = PokerGame.CreateForEvent(PokerGroupId, pokerEventId, CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(PokerGroupId, game.PokerGroupId);
        Assert.Equal(pokerEventId, game.PokerEventId);
        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Create_RequiresPokerGroupId()
    {
        Assert.Throws<ArgumentException>(() => PokerGame.Create(Guid.Empty, CreatedAtUtc));
    }

    [Fact]
    public void CreateForEvent_RequiresPokerEventId()
    {
        Assert.Throws<ArgumentException>(() => PokerGame.CreateForEvent(PokerGroupId, Guid.Empty, CreatedAtUtc));
    }

    [Fact]
    public void AddBuyIn_RecordsBuyInEntry()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();

        var result = game.AddBuyIn(playerId, new Money(50m), RecordedAtUtc);

        Assert.True(result.IsSuccess);
        var entry = result.Value;
        Assert.Equal(playerId, entry.PlayerId);
        Assert.Equal(new Money(50m), entry.Amount);
        Assert.Equal(GameEntryType.BuyIn, entry.Type);
        Assert.Contains(entry, game.Entries);
        Assert.Equal(new Money(50m), game.TotalBuyIns);
    }

    [Fact]
    public void AddCashOut_RecordsCashOutEntry()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(75m), RecordedAtUtc).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(75m), RecordedAtUtc);

        Assert.True(result.IsSuccess);
        var entry = result.Value;
        Assert.Equal(playerId, entry.PlayerId);
        Assert.Equal(new Money(75m), entry.Amount);
        Assert.Equal(GameEntryType.CashOut, entry.Type);
        Assert.Contains(entry, game.Entries);
        Assert.Equal(new Money(75m), game.TotalCashOuts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddBuyIn_RequiresPositiveAmount(decimal amount)
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.AddBuyIn(Guid.NewGuid(), new Money(amount), RecordedAtUtc);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddCashOut_RequiresPositiveAmount(decimal amount)
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.AddCashOut(Guid.NewGuid(), new Money(amount), RecordedAtUtc);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
    }

    [Fact]
    public void AddCashOut_Fails_WhenTotalCashOutsWouldExceedTotalBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(100.01m), RecordedAtUtc);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void AddCashOut_Fails_WhenPlayerHasNoBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerWithBuyIn = Guid.NewGuid();
        var playerWithoutBuyIn = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerWithBuyIn, new Money(100m), RecordedAtUtc).IsSuccess);

        var result = game.AddCashOut(playerWithoutBuyIn, new Money(50m), RecordedAtUtc);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.PlayerHasNoBuyIns, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void AddBuyIn_Fails_WhenRecordedBeforeGameWasCreated()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.AddBuyIn(Guid.NewGuid(), new Money(50m), CreatedAtUtc.AddTicks(-1));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryRecordedBeforeGameCreated, error.Code);
        Assert.Empty(game.Entries);
    }

    [Fact]
    public void AddCashOut_Fails_WhenRecordedBeforeGameWasCreated()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(50m), CreatedAtUtc.AddTicks(-1));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryRecordedBeforeGameCreated, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void ClosedGame_CannotBeModified()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(25m), RecordedAtUtc).IsSuccess);
        Assert.True(game.AddCashOut(playerId, new Money(25m), RecordedAtUtc).IsSuccess);
        Assert.True(game.Close().IsSuccess);

        var buyInResult = game.AddBuyIn(playerId, new Money(10m), RecordedAtUtc);
        var cashOutResult = game.AddCashOut(playerId, new Money(10m), RecordedAtUtc);

        Assert.True(buyInResult.IsFailed);
        Assert.Equal(PokerGameErrorCode.GameClosed, Assert.Single(buyInResult.Errors.OfType<PokerGameError>()).Code);
        Assert.True(cashOutResult.IsFailed);
        Assert.Equal(PokerGameErrorCode.GameClosed, Assert.Single(cashOutResult.Errors.OfType<PokerGameError>()).Code);
    }

    [Fact]
    public void RemoveEntry_RemovesEntry()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m), RecordedAtUtc).Value;

        var result = game.RemoveEntry(entry.Id);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(entry, game.Entries);
        Assert.Equal(Money.Zero, game.TotalBuyIns);
    }

    [Fact]
    public void RemoveEntry_Fails_WhenEntryDoesNotExist()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.RemoveEntry(Guid.NewGuid());

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryNotFound, error.Code);
    }

    [Fact]
    public void RemoveEntry_Fails_WhenGameIsClosed()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m), RecordedAtUtc).Value;
        Assert.True(game.AddCashOut(playerId, new Money(50m), RecordedAtUtc).IsSuccess);
        Assert.True(game.Close().IsSuccess);

        var result = game.RemoveEntry(entry.Id);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.GameClosed, error.Code);
        Assert.Contains(entry, game.Entries);
    }

    [Fact]
    public void UpdateEntryAmount_UpdatesBuyInAmount()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m), RecordedAtUtc).Value;

        var result = game.UpdateEntryAmount(entry.Id, new Money(75m));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(75m), result.Value.Amount);
        Assert.Equal(new Money(75m), game.TotalBuyIns);
    }

    [Fact]
    public void UpdateEntryAmount_UpdatesCashOutAmount()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).IsSuccess);
        var entry = game.AddCashOut(playerId, new Money(50m), RecordedAtUtc).Value;

        var result = game.UpdateEntryAmount(entry.Id, new Money(75m));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(75m), result.Value.Amount);
        Assert.Equal(new Money(75m), game.TotalCashOuts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void UpdateEntryAmount_RequiresPositiveAmount(decimal amount)
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var entry = game.AddBuyIn(Guid.NewGuid(), new Money(50m), RecordedAtUtc).Value;

        var result = game.UpdateEntryAmount(entry.Id, new Money(amount));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
        Assert.Equal(new Money(50m), entry.Amount);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenEntryDoesNotExist()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.UpdateEntryAmount(Guid.NewGuid(), new Money(50m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryNotFound, error.Code);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenGameIsClosed()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m), RecordedAtUtc).Value;
        Assert.True(game.AddCashOut(playerId, new Money(50m), RecordedAtUtc).IsSuccess);
        Assert.True(game.Close().IsSuccess);

        var result = game.UpdateEntryAmount(entry.Id, new Money(75m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.GameClosed, error.Code);
        Assert.Equal(new Money(50m), entry.Amount);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenCashOutsWouldExceedBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).IsSuccess);
        var cashOut = game.AddCashOut(playerId, new Money(75m), RecordedAtUtc).Value;

        var result = game.UpdateEntryAmount(cashOut.Id, new Money(100.01m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.Equal(new Money(75m), cashOut.Amount);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenReducedBuyInWouldMakeCashOutsExceedBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();
        var buyIn = game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).Value;
        Assert.True(game.AddCashOut(playerId, new Money(75m), RecordedAtUtc).IsSuccess);

        var result = game.UpdateEntryAmount(buyIn.Id, new Money(74.99m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.Equal(new Money(100m), buyIn.Amount);
    }

    [Fact]
    public void Close_Fails_WhenBuyInsDoNotEqualCashOuts()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m), RecordedAtUtc).IsSuccess);
        Assert.True(game.AddCashOut(playerId, new Money(80m), RecordedAtUtc).IsSuccess);

        var result = game.Close();

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.BuyInsMustEqualCashOuts, error.Code);
        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Close_Fails_WhenGameHasNoEntries()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);

        var result = game.Close();

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EmptyGame, error.Code);
        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Close_Succeeds_WhenBuyInsEqualCashOuts()
    {
        var game = PokerGame.Create(PokerGroupId, CreatedAtUtc);
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerA, new Money(100m), RecordedAtUtc).IsSuccess);
        Assert.True(game.AddBuyIn(playerB, new Money(50m), RecordedAtUtc).IsSuccess);
        Assert.True(game.AddCashOut(playerA, new Money(25m), RecordedAtUtc).IsSuccess);
        Assert.True(game.AddCashOut(playerB, new Money(125m), RecordedAtUtc).IsSuccess);

        var result = game.Close();

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Closed, game.Status);
    }

}
