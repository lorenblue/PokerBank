using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;

namespace PokerBank.Api.Features.GameResults;

public static class GameResultQuery
{
    public static async Task<GameResultRow[]> ListAsync(
        PokerBankDbContext dbContext,
        Guid pokerGroupId,
        Guid? playerId,
        Guid? gameId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == pokerGroupId)
            .ToGamePlayerTotals(playerId, gameId, closedOnly: true)
            .Join(
                dbContext.Players.AsNoTracking().Where(player => player.PokerGroupId == pokerGroupId),
                result => result.PlayerId,
                player => player.Id,
                (result, player) => new { Result = result, PlayerName = player.Name })
            .OrderByDescending(result => result.Result.PlayedAtUtc)
            .ThenBy(result => result.PlayerName)
            .Select(result => new GameResultRow(
                result.Result.PlayerId,
                result.PlayerName,
                result.Result.GameId,
                result.Result.PlayedAtUtc,
                result.Result.BuyInAmount,
                result.Result.CashOutAmount,
                result.Result.NetAmount))
            .ToArrayAsync(cancellationToken);
    }
}
