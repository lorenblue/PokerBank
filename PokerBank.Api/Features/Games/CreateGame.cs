using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<Created<Response>> Handle(
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = new PokerGame();

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/games/{game.Id}",
            new Response(game.Id, game.Status.ToString(), game.CreatedAtUtc));
    }

    private sealed record Response(Guid Id, string Status, DateTime CreatedAtUtc);
}
