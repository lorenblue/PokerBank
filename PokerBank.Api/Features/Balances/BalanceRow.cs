namespace PokerBank.Api.Features.Balances;

public sealed record BalanceRow(
    Guid PlayerId,
    string PlayerName,
    string? EmailAddress,
    bool IsActive,
    decimal GameNetAmount,
    decimal PaymentNetAmount,
    decimal BalanceAmount);
