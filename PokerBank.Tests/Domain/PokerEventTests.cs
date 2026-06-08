using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PokerEventTests
{
    private static readonly Guid PokerGroupId = Guid.NewGuid();

    [Fact]
    public void Create_ReturnsScheduledEvent()
    {
        var scheduledAtUtc = DateTimeOffset.UtcNow.AddDays(7);

        var result = PokerEvent.Create(PokerGroupId, " Friday poker ", scheduledAtUtc);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(PokerGroupId, result.Value.PokerGroupId);
        Assert.Equal("Friday poker", result.Value.Title);
        Assert.Equal(scheduledAtUtc.ToUniversalTime(), result.Value.ScheduledAtUtc);
        Assert.Equal(PokerEventStatus.Scheduled, result.Value.Status);
        Assert.NotEqual(default, result.Value.CreatedAtUtc);
        Assert.Null(result.Value.CancelledAtUtc);
        Assert.Empty(result.Value.Rsvps);
    }

    [Fact]
    public void Create_RequiresPokerGroupId()
    {
        Assert.Throws<ArgumentException>(() =>
            PokerEvent.Create(Guid.Empty, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Create_RequiresTitle()
    {
        var result = PokerEvent.Create(PokerGroupId, " ", DateTimeOffset.UtcNow.AddDays(7));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerEventError>());
        Assert.Equal(PokerEventErrorCode.InvalidTitle, error.Code);
    }

    [Fact]
    public void Create_RequiresTitleWithinMaximumLength()
    {
        var result = PokerEvent.Create(PokerGroupId, new string('x', PokerEvent.MaxTitleLength + 1), DateTimeOffset.UtcNow.AddDays(7));

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerEventError>());
        Assert.Equal(PokerEventErrorCode.InvalidTitle, error.Code);
    }

    [Fact]
    public void Create_RequiresScheduledTime()
    {
        var result = PokerEvent.Create(PokerGroupId, "Friday poker", default);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerEventError>());
        Assert.Equal(PokerEventErrorCode.InvalidScheduledAt, error.Code);
    }

    [Fact]
    public void UpdateDetails_UpdatesTitleAndScheduledTime()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;
        var rescheduledAtUtc = DateTimeOffset.UtcNow.AddDays(14);

        var result = pokerEvent.UpdateDetails(" Saturday poker ", rescheduledAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal("Saturday poker", pokerEvent.Title);
        Assert.Equal(rescheduledAtUtc.ToUniversalTime(), pokerEvent.ScheduledAtUtc);
    }

    [Fact]
    public void Cancel_CancelsEvent()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;
        var cancelledAtUtc = DateTimeOffset.UtcNow;

        var result = pokerEvent.Cancel(cancelledAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(PokerEventStatus.Cancelled, pokerEvent.Status);
        Assert.Equal(cancelledAtUtc.ToUniversalTime(), pokerEvent.CancelledAtUtc);
    }

    [Fact]
    public void SetRsvp_CreatesRsvp()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;
        var playerId = Guid.NewGuid();
        var respondedAtUtc = DateTimeOffset.UtcNow;

        var result = pokerEvent.SetRsvp(playerId, RsvpStatus.Going, respondedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(pokerEvent.Id, result.Value.EventId);
        Assert.Equal(playerId, result.Value.PlayerId);
        Assert.Equal(RsvpStatus.Going, result.Value.Status);
        Assert.Equal(respondedAtUtc.ToUniversalTime(), result.Value.RespondedAtUtc);
        Assert.Contains(result.Value, pokerEvent.Rsvps);
    }

    [Fact]
    public void SetRsvp_UpdatesExistingRsvpForPlayer()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;
        var playerId = Guid.NewGuid();
        var first = pokerEvent.SetRsvp(playerId, RsvpStatus.Going, DateTimeOffset.UtcNow).Value;

        var result = pokerEvent.SetRsvp(playerId, RsvpStatus.NotGoing, DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Same(first, result.Value);
        Assert.Equal(RsvpStatus.NotGoing, result.Value.Status);
        Assert.Single(pokerEvent.Rsvps);
    }

    [Fact]
    public void SetRsvp_RequiresValidPlayerId()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;

        var result = pokerEvent.SetRsvp(Guid.Empty, RsvpStatus.Going, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerEventError>());
        Assert.Equal(PokerEventErrorCode.InvalidPlayerId, error.Code);
    }

    [Fact]
    public void SetRsvp_RequiresValidStatus()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;

        var result = pokerEvent.SetRsvp(Guid.NewGuid(), (RsvpStatus)0, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PokerEventError>());
        Assert.Equal(PokerEventErrorCode.InvalidRsvpStatus, error.Code);
    }

    [Fact]
    public void CancelledEvent_CannotBeModified()
    {
        var pokerEvent = PokerEvent.Create(PokerGroupId, "Friday poker", DateTimeOffset.UtcNow.AddDays(7)).Value;
        Assert.True(pokerEvent.Cancel(DateTimeOffset.UtcNow).IsSuccess);

        var updateResult = pokerEvent.UpdateDetails("Saturday poker", DateTimeOffset.UtcNow.AddDays(14));
        var rsvpResult = pokerEvent.SetRsvp(Guid.NewGuid(), RsvpStatus.Going, DateTimeOffset.UtcNow);
        var cancelResult = pokerEvent.Cancel(DateTimeOffset.UtcNow);

        Assert.True(updateResult.IsFailed);
        Assert.Equal(PokerEventErrorCode.EventCancelled, Assert.Single(updateResult.Errors.OfType<PokerEventError>()).Code);
        Assert.True(rsvpResult.IsFailed);
        Assert.Equal(PokerEventErrorCode.EventCancelled, Assert.Single(rsvpResult.Errors.OfType<PokerEventError>()).Code);
        Assert.True(cancelResult.IsFailed);
        Assert.Equal(PokerEventErrorCode.EventCancelled, Assert.Single(cancelResult.Errors.OfType<PokerEventError>()).Code);
    }
}
