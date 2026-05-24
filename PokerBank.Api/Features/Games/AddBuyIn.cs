using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class AddBuyIn
{
    public static IEndpointRouteBuilder MapAddBuyIn(this IEndpointRouteBuilder app)
    {
        app.MapPost("/games/{gameId:guid}/buy-ins", Handle)
            .WithName("AddBuyIn")
            .WithTags("Games")
            .WithSummary("Add a buy-in to a game.");

        return app;
    }

    private static async Task<Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid gameId,
        Request request,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .Include(game => game.Entries)
            .SingleOrDefaultAsync(
                game => game.Id == gameId && game.PokerGroupId == currentGroup.Id,
                cancellationToken);

        if (game is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Game was not found."));
        }

        var playerExists = await dbContext.Players.AnyAsync(
            player =>
                player.Id == request.PlayerId &&
                player.PokerGroupId == currentGroup.Id &&
                player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var result = game.AddBuyIn(request.PlayerId, new Money(request.Amount));

        if (result.IsFailed)
        {
            return Failure(result);
        }

        var entry = result.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/games/{game.Id}/entries/{entry.Id}",
            new Response(
                entry.Id,
                game.Id,
                entry.PlayerId,
                entry.Amount.Amount,
                entry.Type,
                entry.RecordedAtUtc));
    }

    private static Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PokerGameError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        if (error?.Code is PokerGameErrorCode.InvalidAmount or PokerGameErrorCode.InvalidPlayerId)
        {
            return TypedResults.BadRequest(new ErrorResponse(message));
        }

        return TypedResults.Conflict(new ErrorResponse(message));
    }

    private sealed record Request(Guid PlayerId, decimal Amount);

    private sealed record Response(
        Guid Id,
        Guid GameId,
        Guid PlayerId,
        decimal Amount,
        GameEntryType Type,
        DateTimeOffset RecordedAtUtc);

}
