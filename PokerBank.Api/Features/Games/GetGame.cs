using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Games;

public static class GetGame
{
    public static IEndpointRouteBuilder MapGetGame(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{id:guid}", Handle)
            .WithName("GetGame")
            .WithTags("Games")
            .WithSummary("Get a game.");

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Id == id)
            .Select(game => new Response(game.Id, game.Status.ToString(), game.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return game is null
            ? Results.NotFound(new ErrorResponse("Game was not found."))
            : Results.Ok(game);
    }

    private sealed record Response(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
