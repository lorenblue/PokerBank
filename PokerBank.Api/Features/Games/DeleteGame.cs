using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class DeleteGame
{
    public static IEndpointRouteBuilder MapDeleteGame(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/games/{id:guid}", Handle)
            .WithName("DeleteGame")
            .WithTags("Games")
            .WithSummary("Delete an open game.");

        return app;
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .Where(game => game.Id == id && game.PokerGroupId == groupContext.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (game is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Game was not found."));
        }

        if (game.Status == GameStatus.Closed)
        {
            return TypedResults.Conflict(new ErrorResponse("Closed games cannot be deleted."));
        }

        dbContext.Games.Remove(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

}
