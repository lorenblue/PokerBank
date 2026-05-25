using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Balances;

public static class BalanceQuery
{
    public static async Task<PlayerBalance[]> ListAsync(
        PokerBankDbContext dbContext,
        Guid pokerGroupId,
        Guid? playerId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var gameNets = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == pokerGroupId)
            .ToGamePlayerTotals(playerId, closedOnly: true)
            .GroupBy(result => result.PlayerId)
            .Select(results => new PlayerAmount(
                results.Key,
                results.Sum(result => result.NetAmount)))
            .ToArrayAsync(cancellationToken);

        var paymentNets = await dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.PokerGroupId == pokerGroupId &&
                (playerId == null || payment.PlayerId == playerId))
            .GroupBy(payment => payment.PlayerId)
            .Select(payments => new PlayerAmount(
                payments.Key,
                payments.Sum(payment => payment.Direction == PaymentDirection.ReceivedByPlayer
                    ? payment.Amount.Amount
                    : -payment.Amount.Amount)))
            .ToArrayAsync(cancellationToken);

        var gameNetsByPlayerId = gameNets.ToDictionary(amount => amount.PlayerId, amount => amount.Amount);
        var paymentNetsByPlayerId = paymentNets.ToDictionary(amount => amount.PlayerId, amount => amount.Amount);

        var playerQuery = dbContext.Players
            .AsNoTracking()
            .Where(player => player.PokerGroupId == pokerGroupId);

        if (playerId is not null)
        {
            playerQuery = playerQuery.Where(player => player.Id == playerId);
        }

        if (activeOnly)
        {
            playerQuery = playerQuery.Where(player => player.IsActive);
        }

        var players = await playerQuery
            .OrderBy(player => player.Name)
            .Select(player => new PlayerProjection(
                player.Id,
                player.Name,
                player.EmailAddress,
                player.IsActive))
            .ToArrayAsync(cancellationToken);

        return players
            .Select(player =>
            {
                var gameNetAmount = gameNetsByPlayerId.GetValueOrDefault(player.Id);
                var paymentNetAmount = paymentNetsByPlayerId.GetValueOrDefault(player.Id);

                return new PlayerBalance(
                    player.Id,
                    player.Name,
                    player.EmailAddress,
                    player.IsActive,
                    gameNetAmount,
                    paymentNetAmount,
                    gameNetAmount - paymentNetAmount);
            })
            .ToArray();
    }

    private sealed record PlayerProjection(Guid Id, string Name, string? EmailAddress, bool IsActive);

    private sealed record PlayerAmount(Guid PlayerId, decimal Amount);
}
