using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class DeleteGameEntry
{
    public static IEndpointRouteBuilder MapDeleteGameEntry(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/games/{gameId:guid}/entries/{entryId:guid}", Handle)
            .WithName("DeleteGameEntry")
            .WithTags("Games")
            .WithSummary("Delete a game entry.");

        return app;
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid gameId,
        Guid entryId,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .Include(game => game.Entries)
            .SingleOrDefaultAsync(game => game.Id == gameId, cancellationToken);

        if (game is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Game was not found."));
        }

        var result = game.RemoveEntry(entryId);

        if (result.IsFailed)
        {
            return Failure(result);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static Results<NoContent, NotFound<ErrorResponse>, Conflict<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PokerGameError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        if (error?.Code == PokerGameErrorCode.EntryNotFound)
        {
            return TypedResults.NotFound(new ErrorResponse(message));
        }

        return TypedResults.Conflict(new ErrorResponse(message));
    }
}
