using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PlayerTests
{
    private static readonly Guid PokerGroupId = Guid.NewGuid();

    [Fact]
    public void NewPlayer_RecordsPlayerDetails()
    {
        var player = new Player(PokerGroupId, "Lorenzo", "lorenzo@example.com");

        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal(PokerGroupId, player.PokerGroupId);
        Assert.Equal("Lorenzo", player.Name);
        Assert.Equal("lorenzo@example.com", player.EmailAddress);
        Assert.Null(player.UserId);
        Assert.True(player.IsActive);
    }

    [Fact]
    public void NewPlayer_RequiresPokerGroupId()
    {
        Assert.Throws<ArgumentException>(() => new Player(Guid.Empty, "Lorenzo"));
    }

    [Fact]
    public void NewPlayer_TrimsName()
    {
        var player = new Player(PokerGroupId, "  Lorenzo  ");

        Assert.Equal("Lorenzo", player.Name);
    }

    [Fact]
    public void NewPlayer_TrimsEmailAddress()
    {
        var player = new Player(PokerGroupId, "Lorenzo", "  lorenzo@example.com  ");

        Assert.Equal("lorenzo@example.com", player.EmailAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NewPlayer_AllowsMissingEmailAddress(string? emailAddress)
    {
        var player = new Player(PokerGroupId, "Lorenzo", emailAddress);

        Assert.Null(player.EmailAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NewPlayer_RequiresName(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Player(PokerGroupId, name!));
    }

    [Fact]
    public void NewPlayer_RequiresNameWithinMaximumLength()
    {
        var name = new string('A', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Player(PokerGroupId, name));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("lorenzo@example")]
    [InlineData("Lorenzo <lorenzo@example.com>")]
    public void NewPlayer_RequiresValidEmailAddress(string emailAddress)
    {
        Assert.Throws<ArgumentException>(() => new Player(PokerGroupId, "Lorenzo", emailAddress));
    }

    [Fact]
    public void NewPlayer_RequiresEmailAddressWithinMaximumLength()
    {
        var emailAddress = $"{new string('a', Player.MaxEmailAddressLength)}@example.com";

        Assert.Throws<ArgumentException>(() => new Player(PokerGroupId, "Lorenzo", emailAddress));
    }

    [Fact]
    public void Rename_ChangesPlayerName()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        player.Rename("Enzo");

        Assert.Equal("Enzo", player.Name);
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        player.Rename("  Enzo  ");

        Assert.Equal("Enzo", player.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_RequiresName(string? name)
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        Assert.Throws<ArgumentException>(() => player.Rename(name!));
    }

    [Fact]
    public void Rename_RequiresNameWithinMaximumLength()
    {
        var player = new Player(PokerGroupId, "Lorenzo");
        var name = new string('A', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => player.Rename(name));
    }

    [Fact]
    public void UpdateEmailAddress_ChangesEmailAddress()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        player.UpdateEmailAddress("enzo@example.com");

        Assert.Equal("enzo@example.com", player.EmailAddress);
    }

    [Fact]
    public void UpdateEmailAddress_TrimsEmailAddress()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        player.UpdateEmailAddress("  enzo@example.com  ");

        Assert.Equal("enzo@example.com", player.EmailAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateEmailAddress_ClearsEmailAddress(string? emailAddress)
    {
        var player = new Player(PokerGroupId, "Lorenzo", "lorenzo@example.com");

        player.UpdateEmailAddress(emailAddress);

        Assert.Null(player.EmailAddress);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("lorenzo@example")]
    [InlineData("Lorenzo <lorenzo@example.com>")]
    public void UpdateEmailAddress_RequiresValidEmailAddress(string emailAddress)
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        Assert.Throws<ArgumentException>(() => player.UpdateEmailAddress(emailAddress));
    }

    [Fact]
    public void Archive_MarksPlayerInactive()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        player.Archive();

        Assert.False(player.IsActive);
    }

    [Fact]
    public void LinkUser_RecordsUserId()
    {
        var player = new Player(PokerGroupId, "Lorenzo");
        var userId = Guid.NewGuid();

        player.LinkUser(userId);

        Assert.Equal(userId, player.UserId);
    }

    [Fact]
    public void LinkUser_RequiresUserId()
    {
        var player = new Player(PokerGroupId, "Lorenzo");

        Assert.Throws<ArgumentException>(() => player.LinkUser(Guid.Empty));
    }
}
