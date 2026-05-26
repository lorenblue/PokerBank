using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;
using PokerBank.Api.Features.GameResults;

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

    private static async Task<Results<Ok<GameResultResponse[]>, NotFound<ErrorResponse>>> Handle(
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

        var results = await GameResultQuery.ListAsync(
            dbContext,
            groupContext.Id,
            currentPlayer.Id,
            gameId,
            cancellationToken);

        return TypedResults.Ok(results
            .Select(GameResultResponse.From)
            .ToArray());
    }
}
