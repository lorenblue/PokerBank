using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PokerGameTests
{
    private static readonly Guid PokerGroupId = Guid.NewGuid();

    [Fact]
    public void Create_StartsOpen()
    {
        var game = PokerGame.Create(PokerGroupId);

        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Create_RecordsGameDetails()
    {
        var game = PokerGame.Create(PokerGroupId);

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(PokerGroupId, game.PokerGroupId);
        Assert.NotEqual(default, game.CreatedAtUtc);
    }

    [Fact]
    public void Create_RequiresPokerGroupId()
    {
        Assert.Throws<ArgumentException>(() => PokerGame.Create(Guid.Empty));
    }

    [Fact]
    public void AddBuyIn_RecordsBuyInEntry()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();

        var result = game.AddBuyIn(playerId, new Money(50m));

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
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(75m)).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(75m));

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
        var game = PokerGame.Create(PokerGroupId);

        var result = game.AddBuyIn(Guid.NewGuid(), new Money(amount));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddCashOut_RequiresPositiveAmount(decimal amount)
    {
        var game = PokerGame.Create(PokerGroupId);

        var result = game.AddCashOut(Guid.NewGuid(), new Money(amount));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
    }

    [Fact]
    public void AddCashOut_Fails_WhenTotalCashOutsWouldExceedTotalBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m)).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(100.01m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void AddCashOut_Fails_WhenPlayerHasNoBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerWithBuyIn = Guid.NewGuid();
        var playerWithoutBuyIn = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerWithBuyIn, new Money(100m)).IsSuccess);

        var result = game.AddCashOut(playerWithoutBuyIn, new Money(50m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.PlayerHasNoBuyIns, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void ClosedGame_CannotBeModified()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(25m)).IsSuccess);
        Assert.True(game.AddCashOut(playerId, new Money(25m)).IsSuccess);
        Assert.True(game.Close().IsSuccess);

        var buyInResult = game.AddBuyIn(playerId, new Money(10m));
        var cashOutResult = game.AddCashOut(playerId, new Money(10m));

        Assert.True(buyInResult.IsFailed);
        Assert.Equal(PokerGameErrorCode.GameClosed, Assert.Single(buyInResult.Errors.OfType<PokerGameError>()).Code);
        Assert.True(cashOutResult.IsFailed);
        Assert.Equal(PokerGameErrorCode.GameClosed, Assert.Single(cashOutResult.Errors.OfType<PokerGameError>()).Code);
    }

    [Fact]
    public void RemoveEntry_RemovesEntry()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m)).Value;

        var result = game.RemoveEntry(entry.Id);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(entry, game.Entries);
        Assert.Equal(Money.Zero, game.TotalBuyIns);
    }

    [Fact]
    public void RemoveEntry_Fails_WhenEntryDoesNotExist()
    {
        var game = PokerGame.Create(PokerGroupId);

        var result = game.RemoveEntry(Guid.NewGuid());

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryNotFound, error.Code);
    }

    [Fact]
    public void RemoveEntry_Fails_WhenGameIsClosed()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m)).Value;
        Assert.True(game.AddCashOut(playerId, new Money(50m)).IsSuccess);
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
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m)).Value;

        var result = game.UpdateEntryAmount(entry.Id, new Money(75m));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(75m), result.Value.Amount);
        Assert.Equal(new Money(75m), game.TotalBuyIns);
    }

    [Fact]
    public void UpdateEntryAmount_UpdatesCashOutAmount()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(100m)).IsSuccess);
        var entry = game.AddCashOut(playerId, new Money(50m)).Value;

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
        var game = PokerGame.Create(PokerGroupId);
        var entry = game.AddBuyIn(Guid.NewGuid(), new Money(50m)).Value;

        var result = game.UpdateEntryAmount(entry.Id, new Money(amount));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
        Assert.Equal(new Money(50m), entry.Amount);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenEntryDoesNotExist()
    {
        var game = PokerGame.Create(PokerGroupId);

        var result = game.UpdateEntryAmount(Guid.NewGuid(), new Money(50m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.EntryNotFound, error.Code);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenGameIsClosed()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        var entry = game.AddBuyIn(playerId, new Money(50m)).Value;
        Assert.True(game.AddCashOut(playerId, new Money(50m)).IsSuccess);
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
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        Assert.True(game.AddBuyIn(playerId, new Money(100m)).IsSuccess);
        var cashOut = game.AddCashOut(playerId, new Money(75m)).Value;

        var result = game.UpdateEntryAmount(cashOut.Id, new Money(100.01m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.Equal(new Money(75m), cashOut.Amount);
    }

    [Fact]
    public void UpdateEntryAmount_Fails_WhenReducedBuyInWouldMakeCashOutsExceedBuyIns()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();
        var buyIn = game.AddBuyIn(playerId, new Money(100m)).Value;
        Assert.True(game.AddCashOut(playerId, new Money(75m)).IsSuccess);

        var result = game.UpdateEntryAmount(buyIn.Id, new Money(74.99m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.Equal(new Money(100m), buyIn.Amount);
    }

    [Fact]
    public void Close_Fails_WhenBuyInsDoNotEqualCashOuts()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m)).IsSuccess);
        Assert.True(game.AddCashOut(playerId, new Money(80m)).IsSuccess);

        var result = game.Close();

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.BuyInsMustEqualCashOuts, error.Code);
        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Close_Succeeds_WhenBuyInsEqualCashOuts()
    {
        var game = PokerGame.Create(PokerGroupId);
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerA, new Money(100m)).IsSuccess);
        Assert.True(game.AddBuyIn(playerB, new Money(50m)).IsSuccess);
        Assert.True(game.AddCashOut(playerA, new Money(25m)).IsSuccess);
        Assert.True(game.AddCashOut(playerB, new Money(125m)).IsSuccess);

        var result = game.Close();

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Closed, game.Status);
    }

}
