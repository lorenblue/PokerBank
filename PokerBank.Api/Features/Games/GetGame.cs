using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

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

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .Include(game => game.Entries)
            .Where(game => game.Id == id && game.PokerGroupId == currentGroup.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (game is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Game was not found."));
        }

        var playerTotals = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Id == id && game.PokerGroupId == currentGroup.Id)
            .ToGamePlayerTotals()
            .Join(
                dbContext.Players.AsNoTracking().Where(player => player.PokerGroupId == currentGroup.Id),
                total => total.PlayerId,
                player => player.Id,
                (total, player) => new { Total = total, PlayerName = player.Name })
            .OrderBy(total => total.PlayerName)
            .Select(total => new PlayerTotalResponse(
                total.Total.PlayerId,
                total.PlayerName,
                total.Total.BuyInAmount,
                total.Total.CashOutAmount,
                total.Total.NetAmount))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new Response(
                game.Id,
                game.Status,
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
                        entry.Type,
                        entry.RecordedAtUtc))
                    .ToArray(),
                playerTotals));
    }

    private sealed record Response(
        Guid Id,
        GameStatus Status,
        DateTime CreatedAtUtc,
        decimal TotalBuyInAmount,
        decimal TotalCashOutAmount,
        decimal RemainingCashOutAmount,
        EntryResponse[] Entries,
        PlayerTotalResponse[] PlayerTotals);

    private sealed record EntryResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        GameEntryType Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record PlayerTotalResponse(
        Guid PlayerId,
        string PlayerName,
        decimal BuyInAmount,
        decimal CashOutAmount,
        decimal NetAmount);

}
