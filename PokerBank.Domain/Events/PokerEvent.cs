using FluentResults;

namespace PokerBank.Domain;

public sealed class PokerEvent
{
    public const int MaxTitleLength = 120;

    private readonly List<EventRsvp> _rsvps = [];

    private PokerEvent()
    {
    }

    public static Result<PokerEvent> Create(
        Guid pokerGroupId,
        string? title,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset createdAtUtc) =>
        Create(Guid.NewGuid(), pokerGroupId, title, scheduledAtUtc, createdAtUtc);

    internal static Result<PokerEvent> Create(
        Guid id,
        Guid pokerGroupId,
        string? title,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.", nameof(id));
        }

        if (pokerGroupId == Guid.Empty)
        {
            throw new ArgumentException("Poker group id is required.", nameof(pokerGroupId));
        }

        var titleResult = NormalizeTitle(title);

        if (titleResult.IsFailed)
        {
            return Result.Fail<PokerEvent>(titleResult.Errors);
        }

        if (scheduledAtUtc == default)
        {
            return Result.Fail<PokerEvent>(PokerEventErrors.InvalidScheduledAt());
        }

        return Result.Ok(new PokerEvent(
            id,
            pokerGroupId,
            titleResult.Value,
            scheduledAtUtc.ToUniversalTime(),
            PokerEventStatus.Scheduled,
            createdAtUtc.ToUniversalTime(),
            cancelledAtUtc: null));
    }

    private PokerEvent(
        Guid id,
        Guid pokerGroupId,
        string title,
        DateTimeOffset scheduledAtUtc,
        PokerEventStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? cancelledAtUtc)
    {
        Id = id;
        PokerGroupId = pokerGroupId;
        Title = title;
        ScheduledAtUtc = scheduledAtUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CancelledAtUtc = cancelledAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PokerGroupId { get; private set; }

    public string Title { get; private set; } = null!;

    public DateTimeOffset ScheduledAtUtc { get; private set; }

    public PokerEventStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<EventRsvp> Rsvps => _rsvps.AsReadOnly();

    public Result UpdateDetails(string? title, DateTimeOffset scheduledAtUtc)
    {
        if (IsCancelled())
        {
            return Result.Fail(PokerEventErrors.EventCancelled());
        }

        var titleResult = NormalizeTitle(title);

        if (titleResult.IsFailed)
        {
            return Result.Fail(titleResult.Errors);
        }

        if (scheduledAtUtc == default)
        {
            return Result.Fail(PokerEventErrors.InvalidScheduledAt());
        }

        Title = titleResult.Value;
        ScheduledAtUtc = scheduledAtUtc.ToUniversalTime();

        return Result.Ok();
    }

    public Result Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (IsCancelled())
        {
            return Result.Fail(PokerEventErrors.EventCancelled());
        }

        Status = PokerEventStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc.ToUniversalTime();

        return Result.Ok();
    }

    public Result<EventRsvp> SetRsvp(Guid playerId, RsvpStatus status, DateTimeOffset respondedAtUtc)
    {
        if (IsCancelled())
        {
            return Result.Fail<EventRsvp>(PokerEventErrors.EventCancelled());
        }

        if (playerId == Guid.Empty)
        {
            return Result.Fail<EventRsvp>(PokerEventErrors.InvalidPlayerId());
        }

        if (!Enum.IsDefined(status))
        {
            return Result.Fail<EventRsvp>(PokerEventErrors.InvalidRsvpStatus());
        }

        var rsvp = _rsvps.SingleOrDefault(rsvp => rsvp.PlayerId == playerId);

        if (rsvp is null)
        {
            rsvp = new EventRsvp(Id, playerId, status, respondedAtUtc);
            _rsvps.Add(rsvp);
        }
        else
        {
            rsvp.Update(status, respondedAtUtc);
        }

        return Result.Ok(rsvp);
    }

    private bool IsCancelled() => Status == PokerEventStatus.Cancelled;

    private static Result<string> NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail<string>(PokerEventErrors.InvalidTitle());
        }

        var normalizedTitle = title.Trim();

        if (normalizedTitle.Length > MaxTitleLength)
        {
            return Result.Fail<string>(PokerEventErrors.TitleTooLong());
        }

        return Result.Ok(normalizedTitle);
    }
}
