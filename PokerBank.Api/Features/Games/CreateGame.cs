using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public static class CreateGame
{
    public static IEndpointRouteBuilder MapCreateGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/games", Handle)
            .WithName("CreateGame")
            .WithTags("Games")
            .WithSummary("Create a game.");

        return app;
    }

    private static async Task<Results<Created<Response>, Conflict<ErrorResponse>>> Handle(
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var openGameExists = await dbContext.Games.AnyAsync(
            game => game.PokerGroupId == groupContext.Id && game.Status == GameStatus.Open,
            cancellationToken);

        if (openGameExists)
        {
            return TypedResults.Conflict(new ErrorResponse("An open game already exists."));
        }

        var game = PokerGame.Create(groupContext.Id, timeProvider.GetUtcNow());

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/games/{game.Id}",
            new Response(game.Id, game.Status, game.CreatedAtUtc));
    }

    private sealed record Response(Guid Id, GameStatus Status, DateTimeOffset CreatedAtUtc);

}
