using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Games;

public static class ListGames
{
    public static IEndpointRouteBuilder MapListGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games", Handle)
            .WithName("ListGames")
            .WithSummary("List games.");

        return app;
    }

    private static async Task<IResult> Handle(
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var games = await dbContext.Games
            .AsNoTracking()
            .OrderByDescending(game => game.CreatedAtUtc)
            .Select(game => new Response(game.Id, game.Status.ToString(), game.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(games);
    }

    private sealed record Response(Guid Id, string Status, DateTime CreatedAtUtc);
}
