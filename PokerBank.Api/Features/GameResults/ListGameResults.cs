using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;

namespace PokerBank.Api.Features.GameResults;

public static class ListGameResults
{
    public static IEndpointRouteBuilder MapListGameResults(this IEndpointRouteBuilder app)
    {
        app.MapGet("/game-results", Handle)
            .WithName("ListGameResults")
            .WithTags("Game Results")
            .WithSummary("List closed-game results.");

        return app;
    }

    private static async Task<Ok<Response[]>> Handle(
        Guid? playerId,
        Guid? gameId,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var results = await dbContext.Games
            .AsNoTracking()
            .ToGameResults(playerId, gameId)
            .Join(
                dbContext.Players.AsNoTracking(),
                result => result.PlayerId,
                player => player.Id,
                (result, player) => new { Result = result, PlayerName = player.Name })
            .OrderByDescending(result => result.Result.PlayedAtUtc)
            .ThenBy(result => result.PlayerName)
            .Select(result => new Response(
                result.Result.PlayerId,
                result.PlayerName,
                result.Result.GameId,
                result.Result.PlayedAtUtc,
                result.Result.BuyInAmount,
                result.Result.CashOutAmount,
                result.Result.NetAmount))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(results);
    }

    private sealed record Response(
        Guid PlayerId,
        string PlayerName,
        Guid GameId,
        DateTime PlayedAtUtc,
        decimal BuyInAmount,
        decimal CashOutAmount,
        decimal NetAmount);
}
