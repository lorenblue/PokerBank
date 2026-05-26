using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class ListPlayers
{
    public static IEndpointRouteBuilder MapListPlayers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players", Handle)
            .WithName("ListPlayers")
            .WithTags("Players")
            .WithSummary("List players.");

        return app;
    }

    private static async Task<Ok<PlayerResponse[]>> Handle(
        bool? includeArchived,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Players
            .AsNoTracking()
            .Where(player => player.PokerGroupId == groupContext.Id);

        if (includeArchived is not true)
        {
            query = query.Where(player => player.IsActive);
        }

        var players = await query
            .OrderBy(player => player.Name)
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(players
            .Select(PlayerResponse.From)
            .ToArray());
    }
}
