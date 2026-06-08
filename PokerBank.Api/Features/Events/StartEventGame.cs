using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Events;

public static class StartEventGame
{
    public static IEndpointRouteBuilder MapStartEventGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/game", Handle)
            .WithName("StartEventGame")
            .WithTags("Events")
            .WithSummary("Start a game from an event.");

        return app;
    }

    private static async Task<Results<Created<Response>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pokerEvent = await dbContext.Events
            .AsNoTracking()
            .Where(pokerEvent => pokerEvent.Id == id && pokerEvent.PokerGroupId == groupContext.Id)
            .Select(pokerEvent => new
            {
                pokerEvent.Id,
                pokerEvent.Status,
                pokerEvent.ScheduledAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (pokerEvent is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Event was not found."));
        }

        if (pokerEvent.Status == PokerEventStatus.Cancelled)
        {
            return TypedResults.Conflict(new ErrorResponse("Cancelled events cannot start games."));
        }

        var existingEventGameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == groupContext.Id && game.PokerEventId == pokerEvent.Id)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingEventGameId is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("A game already exists for this event."));
        }

        if (pokerEvent.ScheduledAtUtc > DateTimeOffset.UtcNow)
        {
            return TypedResults.Conflict(new ErrorResponse("Cannot start a game before the event's scheduled time."));
        }

        var openGameExists = await dbContext.Games.AnyAsync(
            game => game.PokerGroupId == groupContext.Id && game.Status == GameStatus.Open,
            cancellationToken);

        if (openGameExists)
        {
            return TypedResults.Conflict(new ErrorResponse("An open game already exists."));
        }

        var game = PokerGame.CreateForEvent(groupContext.Id, pokerEvent.Id);

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/games/{game.Id}",
            new Response(game.Id, game.PokerEventId, game.Status, game.CreatedAtUtc));
    }

    private sealed record Response(Guid Id, Guid? PokerEventId, GameStatus Status, DateTime CreatedAtUtc);
}
