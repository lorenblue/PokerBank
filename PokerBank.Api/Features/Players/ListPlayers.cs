using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class ListPlayers
{
    public static IEndpointRouteBuilder MapListPlayers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players", Handle)
            .WithName("ListPlayers")
            .WithSummary("List players.");

        return app;
    }

    private static async Task<IResult> Handle(
        bool? includeArchived,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Players.AsNoTracking();

        if (includeArchived is not true)
        {
            query = query.Where(player => player.IsActive);
        }

        var players = await query
            .OrderBy(player => player.Name)
            .Select(player => new Response(player.Id, player.Name, player.IsActive))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(players);
    }

    private sealed record Response(Guid Id, string Name, bool IsActive);
}
