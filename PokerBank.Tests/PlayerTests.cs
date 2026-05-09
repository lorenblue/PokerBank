using PokerBank.Domain;

namespace PokerBank.Tests;

public sealed class PlayerTests
{
    [Fact]
    public void NewPlayer_RecordsPlayerDetails()
    {
        var player = new Player("Lorenzo");

        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("Lorenzo", player.Name);
    }

    [Fact]
    public void NewPlayer_TrimsName()
    {
        var player = new Player("  Lorenzo  ");

        Assert.Equal("Lorenzo", player.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void NewPlayer_RequiresName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Player(name));
    }

    [Fact]
    public void NewPlayer_RequiresNameWithinMaximumLength()
    {
        var name = new string('A', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Player(name));
    }
}
