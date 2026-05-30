using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PlayerInvitationTests
{
    private static readonly Guid PokerGroupId = Guid.NewGuid();
    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAtUtc = Now.AddDays(7);

    [Fact]
    public void Create_ReturnsInvitation()
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            "token-hash",
            Now,
            ExpiresAtUtc);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(PokerGroupId, result.Value.PokerGroupId);
        Assert.Equal(PlayerId, result.Value.PlayerId);
        Assert.Equal("lorenzo@example.com", result.Value.EmailAddress);
        Assert.Equal("token-hash", result.Value.TokenHash);
        Assert.Equal(Now, result.Value.CreatedAtUtc);
        Assert.Equal(ExpiresAtUtc, result.Value.ExpiresAtUtc);
        Assert.Null(result.Value.AcceptedAtUtc);
        Assert.False(result.Value.IsAccepted);
    }

    [Fact]
    public void Create_TrimsAndNormalizesEmailAddress()
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "  Lorenzo@Example.com  ",
            "token-hash",
            Now,
            ExpiresAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal("lorenzo@example.com", result.Value.EmailAddress);
    }

    [Fact]
    public void Create_TrimsTokenHash()
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            "  token-hash  ",
            Now,
            ExpiresAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal("token-hash", result.Value.TokenHash);
    }

    [Fact]
    public void Create_RequiresPokerGroupId()
    {
        var result = PlayerInvitation.Create(
            Guid.Empty,
            PlayerId,
            "lorenzo@example.com",
            "token-hash",
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidPokerGroupId);
    }

    [Fact]
    public void Create_RequiresPlayerId()
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            Guid.Empty,
            "lorenzo@example.com",
            "token-hash",
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidPlayerId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("lorenzo@example")]
    [InlineData("Lorenzo <lorenzo@example.com>")]
    public void Create_RequiresValidEmailAddress(string? emailAddress)
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            emailAddress,
            "token-hash",
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidEmailAddress);
    }

    [Fact]
    public void Create_RequiresEmailAddressWithinMaximumLength()
    {
        var emailAddress = $"{new string('a', PlayerInvitation.MaxEmailAddressLength)}@example.com";

        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            emailAddress,
            "token-hash",
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidEmailAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_RequiresTokenHash(string? tokenHash)
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            tokenHash,
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidTokenHash);
    }

    [Fact]
    public void Create_RequiresTokenHashWithinMaximumLength()
    {
        var tokenHash = new string('a', PlayerInvitation.MaxTokenHashLength + 1);

        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            tokenHash,
            Now,
            ExpiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidTokenHash);
    }

    [Theory]
    [MemberData(nameof(InvalidExpirations))]
    public void Create_RequiresFutureExpiration(DateTimeOffset expiresAtUtc)
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            "token-hash",
            Now,
            expiresAtUtc);

        AssertFailure(result, PlayerInvitationErrorCode.InvalidExpiration);
    }

    [Fact]
    public void Accept_SetsAcceptedAtUtc()
    {
        var invitation = CreateInvitation();
        var acceptedAtUtc = Now.AddDays(1);

        var result = invitation.Accept(acceptedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(acceptedAtUtc, invitation.AcceptedAtUtc);
        Assert.True(invitation.IsAccepted);
    }

    [Theory]
    [MemberData(nameof(ExpiredAcceptanceTimes))]
    public void Accept_Fails_WhenInvitationIsExpired(DateTimeOffset acceptedAtUtc)
    {
        var invitation = CreateInvitation();

        var result = invitation.Accept(acceptedAtUtc);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PlayerInvitationError>());
        Assert.Equal(PlayerInvitationErrorCode.Expired, error.Code);
        Assert.Null(invitation.AcceptedAtUtc);
        Assert.False(invitation.IsAccepted);
    }

    [Fact]
    public void Accept_Fails_WhenInvitationIsAlreadyAccepted()
    {
        var invitation = CreateInvitation();

        invitation.Accept(Now.AddDays(1));
        var result = invitation.Accept(Now.AddDays(2));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PlayerInvitationError>());
        Assert.Equal(PlayerInvitationErrorCode.AlreadyAccepted, error.Code);
        Assert.Equal(Now.AddDays(1), invitation.AcceptedAtUtc);
    }

    public static TheoryData<DateTimeOffset> InvalidExpirations() => new()
    {
        Now.AddTicks(-1),
        Now
    };

    public static TheoryData<DateTimeOffset> ExpiredAcceptanceTimes() => new()
    {
        ExpiresAtUtc,
        ExpiresAtUtc.AddTicks(1)
    };

    private static PlayerInvitation CreateInvitation()
    {
        var result = PlayerInvitation.Create(
            PokerGroupId,
            PlayerId,
            "lorenzo@example.com",
            "token-hash",
            Now,
            ExpiresAtUtc);

        return result.Value;
    }

    private static void AssertFailure<T>(FluentResults.Result<T> result, PlayerInvitationErrorCode expectedCode)
    {
        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PlayerInvitationError>());
        Assert.Equal(expectedCode, error.Code);
    }
}
