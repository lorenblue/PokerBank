using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PokerGameTests
{
    [Fact]
    public void NewGame_StartsOpen()
    {
        var game = new PokerGame();

        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void NewGame_RecordsGameDetails()
    {
        var game = new PokerGame();

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.NotEqual(default, game.CreatedAtUtc);
    }

    [Fact]
    public void AddBuyIn_RecordsBuyInEntry()
    {
        var game = new PokerGame();
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
        var game = new PokerGame();
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
        var game = new PokerGame();

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
        var game = new PokerGame();

        var result = game.AddCashOut(Guid.NewGuid(), new Money(amount));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.InvalidAmount, error.Code);
    }

    [Fact]
    public void AddCashOut_Fails_WhenTotalCashOutsWouldExceedTotalBuyIns()
    {
        var game = new PokerGame();
        var playerId = Guid.NewGuid();

        Assert.True(game.AddBuyIn(playerId, new Money(100m)).IsSuccess);

        var result = game.AddCashOut(playerId, new Money(100.01m));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerGameError>());
        Assert.Equal(PokerGameErrorCode.CashOutsExceedBuyIns, error.Code);
        Assert.DoesNotContain(game.Entries, entry => entry.Type == GameEntryType.CashOut);
    }

    [Fact]
    public void ClosedGame_CannotBeModified()
    {
        var game = new PokerGame();
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
    public void Close_Fails_WhenBuyInsDoNotEqualCashOuts()
    {
        var game = new PokerGame();
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
        var game = new PokerGame();
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
