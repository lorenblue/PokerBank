using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Me;

public sealed class CurrentPlayerProvider(
    IHttpContextAccessor httpContextAccessor,
    IPokerGroupContext groupContext,
    PokerBankDbContext dbContext) : ICurrentPlayerProvider
{
    public async Task<CurrentPlayer?> GetAsync(CancellationToken cancellationToken)
    {
        var userIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var playerId = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.PokerGroupId == groupContext.Id && player.UserId == userId)
            .Select(player => (Guid?)player.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return playerId is null
            ? null
            : new CurrentPlayer(playerId.Value);
    }
}
