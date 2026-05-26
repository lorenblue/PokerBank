namespace PokerBank.Api.Features.GameResults;

public sealed record GameResultRow(
    Guid PlayerId,
    string PlayerName,
    Guid GameId,
    DateTime PlayedAtUtc,
    decimal BuyInAmount,
    decimal CashOutAmount,
    decimal NetAmount);
