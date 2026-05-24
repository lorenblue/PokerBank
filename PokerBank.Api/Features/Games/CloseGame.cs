using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

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

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid gameId,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .Include(game => game.Entries)
            .SingleOrDefaultAsync(
                game => game.Id == gameId && game.PokerGroupId == groupContext.Id,
                cancellationToken);

        if (game is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Game was not found."));
        }

        var result = game.Close();

        if (result.IsFailed)
        {
            return Failure(result);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(game.Id, game.Status, game.CreatedAtUtc));
    }

    private static Results<Ok<Response>, NotFound<ErrorResponse>, Conflict<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PokerGameError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        return TypedResults.Conflict(new ErrorResponse(message));
    }

    private sealed record Response(Guid Id, GameStatus Status, DateTime CreatedAtUtc);

}
