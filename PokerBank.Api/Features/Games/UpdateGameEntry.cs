using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class UpdateGameEntry
{
    public static IEndpointRouteBuilder MapUpdateGameEntry(this IEndpointRouteBuilder app)
    {
        app.MapPut("/games/{gameId:guid}/entries/{entryId:guid}", Handle)
            .WithName("UpdateGameEntry")
            .WithTags("Games")
            .WithSummary("Update a game entry.");

        return app;
    }

    private static async Task<Results<Ok<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid gameId,
        Guid entryId,
        Request request,
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

        var result = game.UpdateEntryAmount(entryId, new Money(request.Amount));

        if (result.IsFailed)
        {
            return Failure(result);
        }

        var entry = result.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            entry.Id,
            game.Id,
            entry.PlayerId,
            entry.Amount.Amount,
            entry.Type,
            entry.RecordedAtUtc));
    }

    private static Results<Ok<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PokerGameError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        if (error?.Code == PokerGameErrorCode.InvalidAmount)
        {
            return TypedResults.BadRequest(new ErrorResponse(message));
        }

        if (error?.Code == PokerGameErrorCode.EntryNotFound)
        {
            return TypedResults.NotFound(new ErrorResponse(message));
        }

        return TypedResults.Conflict(new ErrorResponse(message));
    }

    private sealed record Request(decimal Amount);

    private sealed record Response(
        Guid Id,
        Guid GameId,
        Guid PlayerId,
        decimal Amount,
        GameEntryType Type,
        DateTimeOffset RecordedAtUtc);
}
