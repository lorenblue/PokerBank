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

    private static async Task<IResult> Handle(
        Guid gameId,
        Request request,
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

        var playerExists = await dbContext.Players.AnyAsync(
            player => player.Id == request.PlayerId && player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return Results.NotFound(new ErrorResponse("Player was not found."));
        }

        var result = game.AddBuyIn(request.PlayerId, new Money(request.Amount));

        if (result.IsFailed)
        {
            return result.ToApiError();
        }

        var entry = result.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/games/{game.Id}/entries/{entry.Id}",
            new Response(
                entry.Id,
                game.Id,
                entry.PlayerId,
                entry.Amount.Amount,
                entry.Type.ToString(),
                entry.RecordedAtUtc));
    }

    private sealed record Request(Guid PlayerId, decimal Amount);

    private sealed record Response(
        Guid Id,
        Guid GameId,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record ErrorResponse(string Error);
}
