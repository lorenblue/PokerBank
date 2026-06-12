namespace PokerBank.Api.Features.GameResults;

public sealed record GameResultResponse(
    Guid PlayerId,
    string PlayerName,
    Guid GameId,
    DateTimeOffset PlayedAtUtc,
    decimal BuyInAmount,
    decimal CashOutAmount,
    decimal NetAmount)
{
    public static GameResultResponse From(GameResultRow row) =>
        new(
            row.PlayerId,
            row.PlayerName,
            row.GameId,
            row.PlayedAtUtc,
            row.BuyInAmount,
            row.CashOutAmount,
            row.NetAmount);
}
