using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PokerGroupTests
{
    [Fact]
    public void NewPokerGroup_RecordsGroupDetails()
    {
        var group = new PokerGroup("Friday Poker");

        Assert.NotEqual(Guid.Empty, group.Id);
        Assert.Equal("Friday Poker", group.Name);
        Assert.True(group.IsActive);
    }

    [Fact]
    public void NewPokerGroup_TrimsName()
    {
        var group = new PokerGroup("  Friday Poker  ");

        Assert.Equal("Friday Poker", group.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NewPokerGroup_RequiresName(string? name)
    {
        Assert.Throws<ArgumentException>(() => new PokerGroup(name!));
    }

    [Fact]
    public void NewPokerGroup_RequiresNameWithinMaximumLength()
    {
        var name = new string('A', PokerGroup.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new PokerGroup(name));
    }

    [Fact]
    public void Rename_ChangesGroupName()
    {
        var group = new PokerGroup("Friday Poker");

        group.Rename("Saturday Poker");

        Assert.Equal("Saturday Poker", group.Name);
    }

    [Fact]
    public void Archive_MarksGroupInactive()
    {
        var group = new PokerGroup("Friday Poker");

        group.Archive();

        Assert.False(group.IsActive);
    }
}
