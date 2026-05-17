using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

internal static class GamePlayerTotalQueryExtensions
{
    public static IQueryable<GamePlayerTotalProjection> ToGamePlayerTotals(
        this IQueryable<PokerGame> games,
        Guid? playerId = null,
        Guid? gameId = null,
        bool closedOnly = false)
    {
        if (closedOnly)
        {
            games = games.Where(game => game.Status == GameStatus.Closed);
        }

        var entries = games
            .SelectMany(game => game.Entries.Select(entry => new
            {
                GameId = game.Id,
                PlayedAtUtc = game.CreatedAtUtc,
                entry.PlayerId,
                entry.Type,
                Amount = entry.Amount.Amount
            }));

        if (playerId is not null)
        {
            entries = entries.Where(entry => entry.PlayerId == playerId);
        }

        if (gameId is not null)
        {
            entries = entries.Where(entry => entry.GameId == gameId);
        }

        return entries
            .GroupBy(entry => new
            {
                entry.GameId,
                entry.PlayedAtUtc,
                entry.PlayerId
            })
            .Select(entries => new GamePlayerTotalProjection
            {
                PlayerId = entries.Key.PlayerId,
                GameId = entries.Key.GameId,
                PlayedAtUtc = entries.Key.PlayedAtUtc,
                BuyInAmount = entries.Sum(entry => entry.Type == GameEntryType.BuyIn ? entry.Amount : 0m),
                CashOutAmount = entries.Sum(entry => entry.Type == GameEntryType.CashOut ? entry.Amount : 0m),
                NetAmount = entries.Sum(entry => entry.Type == GameEntryType.CashOut ? entry.Amount : -entry.Amount)
            });
    }
}

internal sealed class GamePlayerTotalProjection
{
    public Guid PlayerId { get; init; }

    public Guid GameId { get; init; }

    public DateTime PlayedAtUtc { get; init; }

    public decimal BuyInAmount { get; init; }

    public decimal CashOutAmount { get; init; }

    public decimal NetAmount { get; init; }
}
