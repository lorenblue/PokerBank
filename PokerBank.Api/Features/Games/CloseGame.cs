using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Games;

public static class CloseGame
{
    public static IEndpointRouteBuilder MapCloseGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/games/{gameId:guid}/close", Handle)
            .WithName("CloseGame")
            .WithTags("Games")
            .WithSummary("Close a game.");

        return app;
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .Include(game => game.Entries)
            .SingleOrDefaultAsync(game => game.Id == gameId, cancellationToken);

        if (game is null)
        {
            return Results.NotFound(new ErrorResponse("Game was not found."));
        }

        var result = game.Close();

        if (result.IsFailed)
        {
            return result.ToApiError();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new Response(game.Id, game.Status.ToString(), game.CreatedAtUtc));
    }

    private sealed record Response(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
