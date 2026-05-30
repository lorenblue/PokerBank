using Microsoft.EntityFrameworkCore;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Players;

internal static class PlayerEmailQueryExtensions
{
    public static Task<bool> ActivePlayerEmailExistsAsync(
        this IQueryable<Player> players,
        Guid pokerGroupId,
        string emailAddress,
        Guid? excludingPlayerId,
        CancellationToken cancellationToken)
    {
        return players.AnyAsync(
            player =>
                player.PokerGroupId == pokerGroupId &&
                player.IsActive &&
                player.EmailAddress == emailAddress &&
                (excludingPlayerId == null || player.Id != excludingPlayerId),
            cancellationToken);
    }
}
