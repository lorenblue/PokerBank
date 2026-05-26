namespace PokerBank.Api.Features.Balances;

public sealed record BalanceResponse(
    Guid PlayerId,
    string PlayerName,
    bool IsActive,
    decimal GameNetAmount,
    decimal PaymentNetAmount,
    decimal BalanceAmount)
{
    public static BalanceResponse From(BalanceRow row) =>
        new(
            row.PlayerId,
            row.PlayerName,
            row.IsActive,
            row.GameNetAmount,
            row.PaymentNetAmount,
            row.BalanceAmount);
}
