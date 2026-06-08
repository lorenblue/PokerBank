namespace PokerBank.Tests.TestSupport;

internal sealed record PlayerResponse(Guid Id, string Name, string? EmailAddress, bool IsActive);

internal sealed record PlayerDetailsResponse(
    Guid Id,
    string Name,
    string? EmailAddress,
    bool IsActive,
    bool HasUserAccount,
    PendingInvitationResponse? PendingInvitation);

internal sealed record PendingInvitationResponse(
    Guid Id,
    string EmailAddress,
    DateTimeOffset ExpiresAtUtc);

internal sealed record MyProfileResponse(
    Guid Id,
    string Name,
    string? EmailAddress,
    bool IsActive);

internal sealed record EventResponse(
    Guid Id,
    string Title,
    DateTimeOffset ScheduledAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    int GoingCount,
    int MaybeCount,
    int NotGoingCount,
    string? MyRsvpStatus);

internal sealed record EventDetailsResponse(
    Guid Id,
    string Title,
    DateTimeOffset ScheduledAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    int GoingCount,
    int MaybeCount,
    int NotGoingCount,
    string? MyRsvpStatus,
    EventRsvpResponse[] Rsvps);

internal sealed record EventRsvpResponse(
    Guid PlayerId,
    string PlayerName,
    string Status,
    DateTimeOffset RespondedAtUtc);

internal sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

internal sealed record PaymentResponse(
    Guid Id,
    Guid PlayerId,
    decimal Amount,
    string Direction,
    string Method,
    DateTimeOffset RecordedAtUtc);

internal sealed record ListPaymentsResponse(
    PaymentResponse[] Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

internal sealed record BalanceResponse(
    Guid PlayerId,
    string PlayerName,
    bool IsActive,
    decimal GameNetAmount,
    decimal PaymentNetAmount,
    decimal BalanceAmount);

internal sealed record GameResultResponse(
    Guid PlayerId,
    string PlayerName,
    Guid GameId,
    DateTime PlayedAtUtc,
    decimal BuyInAmount,
    decimal CashOutAmount,
    decimal NetAmount);

internal sealed record MyGameResponse(
    Guid Id,
    string Status,
    DateTime PlayedAtUtc,
    decimal MyBuyInAmount,
    decimal MyCashOutAmount,
    decimal MyNetAmount,
    int PlayerCount,
    decimal TotalBuyInAmount,
    decimal TotalCashOutAmount);

internal sealed record InvitePlayerResponse(
    Guid Id,
    Guid PlayerId,
    string EmailAddress,
    DateTimeOffset ExpiresAtUtc);

internal sealed record SendBalanceUpdatesResponse(int Sent, SkippedPlayerResponse[] Skipped);

internal sealed record SkippedPlayerResponse(Guid PlayerId, string PlayerName, string Reason);

internal sealed record ErrorResponse(string Error);
