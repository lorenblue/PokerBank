using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;

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

    private static async Task<Ok<GameResultResponse[]>> Handle(
        Guid? playerId,
        Guid? gameId,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var results = await GameResultQuery.ListAsync(
            dbContext,
            groupContext.Id,
            playerId,
            gameId,
            cancellationToken);

        return TypedResults.Ok(results
            .Select(GameResultResponse.From)
            .ToArray());
    }
}
