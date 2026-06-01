namespace PokerBank.Tests.TestSupport;

internal sealed record PlayerResponse(Guid Id, string Name, string? EmailAddress, bool IsActive);

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
