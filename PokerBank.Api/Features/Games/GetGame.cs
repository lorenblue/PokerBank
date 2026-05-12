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
            .Include(game => game.Entries)
            .Where(game => game.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        return game is null
            ? Results.NotFound(new ErrorResponse("Game was not found."))
            : Results.Ok(new Response(
                game.Id,
                game.Status.ToString(),
                game.CreatedAtUtc,
                game.TotalBuyIns.Amount,
                game.TotalCashOuts.Amount,
                (game.TotalBuyIns - game.TotalCashOuts).Amount,
                game.Entries
                    .OrderBy(entry => entry.RecordedAtUtc)
                    .Select(entry => new EntryResponse(
                        entry.Id,
                        entry.PlayerId,
                        entry.Amount.Amount,
                        entry.Type.ToString(),
                        entry.RecordedAtUtc))
                    .ToArray()));
    }

    private sealed record Response(
        Guid Id,
        string Status,
        DateTime CreatedAtUtc,
        decimal TotalBuyInAmount,
        decimal TotalCashOutAmount,
        decimal RemainingCashOutAmount,
        EntryResponse[] Entries);

    private sealed record EntryResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record ErrorResponse(string Error);
}
