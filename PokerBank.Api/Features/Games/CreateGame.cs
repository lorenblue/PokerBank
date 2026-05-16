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
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var openGameExists = await dbContext.Games.AnyAsync(
            game => game.Status == GameStatus.Open,
            cancellationToken);

        if (openGameExists)
        {
            return TypedResults.Conflict(new ErrorResponse("An open game already exists."));
        }

        var game = PokerGame.Create();

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/games/{game.Id}",
            new Response(game.Id, game.Status.ToString(), game.CreatedAtUtc));
    }

    private sealed record Response(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
