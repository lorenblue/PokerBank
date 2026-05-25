namespace PokerBank.Api.Features.Balances;

public sealed record PlayerBalance(
    Guid PlayerId,
    string PlayerName,
    string? EmailAddress,
    bool IsActive,
    decimal GameNetAmount,
    decimal PaymentNetAmount,
    decimal BalanceAmount);
