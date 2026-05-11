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

        var entry = game.AddBuyIn(playerId, new Money(50m));

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

        var entry = game.AddCashOut(playerId, new Money(75m));

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

        Assert.Throws<ArgumentOutOfRangeException>(() => game.AddBuyIn(Guid.NewGuid(), new Money(amount)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddCashOut_RequiresPositiveAmount(decimal amount)
    {
        var game = new PokerGame();

        Assert.Throws<ArgumentOutOfRangeException>(() => game.AddCashOut(Guid.NewGuid(), new Money(amount)));
    }

    [Fact]
    public void ClosedGame_CannotBeModified()
    {
        var game = new PokerGame();
        var playerId = Guid.NewGuid();

        game.AddBuyIn(playerId, new Money(25m));
        game.AddCashOut(playerId, new Money(25m));
        game.Close();

        Assert.Throws<InvalidOperationException>(() => game.AddBuyIn(playerId, new Money(10m)));
        Assert.Throws<InvalidOperationException>(() => game.AddCashOut(playerId, new Money(10m)));
    }

    [Fact]
    public void Close_Fails_WhenBuyInsDoNotEqualCashOuts()
    {
        var game = new PokerGame();
        var playerId = Guid.NewGuid();

        game.AddBuyIn(playerId, new Money(100m));
        game.AddCashOut(playerId, new Money(80m));

        Assert.Throws<InvalidOperationException>(game.Close);
        Assert.Equal(GameStatus.Open, game.Status);
    }

    [Fact]
    public void Close_Succeeds_WhenBuyInsEqualCashOuts()
    {
        var game = new PokerGame();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        game.AddBuyIn(playerA, new Money(100m));
        game.AddBuyIn(playerB, new Money(50m));
        game.AddCashOut(playerA, new Money(25m));
        game.AddCashOut(playerB, new Money(125m));

        game.Close();

        Assert.Equal(GameStatus.Closed, game.Status);
    }

    [Fact]
    public void GetNetForPlayer_ReturnsCashOutsMinusBuyIns()
    {
        var game = new PokerGame();
        var playerId = Guid.NewGuid();

        game.AddBuyIn(playerId, new Money(100m));
        game.AddBuyIn(playerId, new Money(50m));
        game.AddCashOut(playerId, new Money(120m));

        Assert.Equal(new Money(-30m), game.GetNetForPlayer(playerId));
    }

    [Fact]
    public void ClosedGame_PlayerNetsSumToZero()
    {
        var game = new PokerGame();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var playerC = Guid.NewGuid();

        game.AddBuyIn(playerA, new Money(100m));
        game.AddBuyIn(playerB, new Money(100m));
        game.AddBuyIn(playerC, new Money(100m));
        game.AddCashOut(playerA, new Money(250m));
        game.AddCashOut(playerB, new Money(50m));
        game.AddCashOut(playerC, new Money(0.01m));
        game.AddBuyIn(playerC, new Money(0.01m));

        game.Close();

        var totalNet = game.GetPlayerNets().Values.Aggregate(Money.Zero, (total, net) => total + net);
        Assert.Equal(Money.Zero, totalNet);
    }
}
