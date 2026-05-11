using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PlayerTests
{
    [Fact]
    public void NewPlayer_RecordsPlayerDetails()
    {
        var player = new Player("Lorenzo");

        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("Lorenzo", player.Name);
        Assert.True(player.IsActive);
    }

    [Fact]
    public void NewPlayer_TrimsName()
    {
        var player = new Player("  Lorenzo  ");

        Assert.Equal("Lorenzo", player.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NewPlayer_RequiresName(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Player(name!));
    }

    [Fact]
    public void NewPlayer_RequiresNameWithinMaximumLength()
    {
        var name = new string('A', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Player(name));
    }

    [Fact]
    public void Rename_ChangesPlayerName()
    {
        var player = new Player("Lorenzo");

        player.Rename("Enzo");

        Assert.Equal("Enzo", player.Name);
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var player = new Player("Lorenzo");

        player.Rename("  Enzo  ");

        Assert.Equal("Enzo", player.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_RequiresName(string? name)
    {
        var player = new Player("Lorenzo");

        Assert.Throws<ArgumentException>(() => player.Rename(name!));
    }

    [Fact]
    public void Rename_RequiresNameWithinMaximumLength()
    {
        var player = new Player("Lorenzo");
        var name = new string('A', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => player.Rename(name));
    }

    [Fact]
    public void Archive_MarksPlayerInactive()
    {
        var player = new Player("Lorenzo");

        player.Archive();

        Assert.False(player.IsActive);
    }
}
