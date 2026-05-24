using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class ListGames
{
    public static IEndpointRouteBuilder MapListGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games", Handle)
            .WithName("ListGames")
            .WithTags("Games")
            .WithSummary("List games.");

        return app;
    }

    private static async Task<Ok<Response[]>> Handle(
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var games = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == currentGroup.Id)
            .OrderByDescending(game => game.CreatedAtUtc)
            .Select(game => new Response(game.Id, game.Status, game.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(games);
    }

    private sealed record Response(Guid Id, GameStatus Status, DateTime CreatedAtUtc);
}
