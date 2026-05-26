using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;

namespace PokerBank.Api.Features.Me;

public static class GetMyGameResults
{
    public static IEndpointRouteBuilder MapGetMyGameResults(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/game-results", Handle)
            .WithName("GetMyGameResults")
            .WithTags("Me")
            .WithSummary("Get my closed-game results.");

        return app;
    }

    private static async Task<Results<Ok<Response[]>, NotFound<ErrorResponse>>> Handle(
        Guid? gameId,
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);

        if (currentPlayer is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player profile was not found."));
        }

        var results = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == groupContext.Id)
            .ToGamePlayerTotals(currentPlayer.Id, gameId, closedOnly: true)
            .Join(
                dbContext.Players.AsNoTracking().Where(player => player.PokerGroupId == groupContext.Id),
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
