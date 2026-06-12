using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Me;

public static class GetMyGames
{
    public static IEndpointRouteBuilder MapGetMyGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/games", Handle)
            .WithName("GetMyGames")
            .WithTags("Me")
            .WithSummary("Get my games.");

        return app;
    }

    private static async Task<Results<Ok<Response[]>, NotFound<ErrorResponse>>> Handle(
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);

        if (currentPlayer is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player profile was not found."));
        }

        var groupGames = dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == groupContext.Id);

        var myTotals = groupGames.ToGamePlayerTotals(playerId: currentPlayer.Id);

        var gameTotals = groupGames
            .ToGamePlayerTotals()
            .GroupBy(total => total.GameId)
            .Select(totals => new
            {
                GameId = totals.Key,
                PlayerCount = totals.Count(),
                TotalBuyInAmount = totals.Sum(total => total.BuyInAmount),
                TotalCashOutAmount = totals.Sum(total => total.CashOutAmount)
            });

        var games = await myTotals
            .Join(
                groupGames,
                myTotal => myTotal.GameId,
                game => game.Id,
                (myTotal, game) => new { MyTotal = myTotal, game.Status })
            .Join(
                gameTotals,
                game => game.MyTotal.GameId,
                total => total.GameId,
                (game, total) => new { game.MyTotal, game.Status, Total = total })
            .OrderByDescending(game => game.MyTotal.PlayedAtUtc)
            .Select(game => new Response(
                game.MyTotal.GameId,
                game.Status,
                game.MyTotal.PlayedAtUtc,
                game.MyTotal.BuyInAmount,
                game.MyTotal.CashOutAmount,
                game.MyTotal.NetAmount,
                game.Total.PlayerCount,
                game.Total.TotalBuyInAmount,
                game.Total.TotalCashOutAmount))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(games);
    }

    private sealed record Response(
        Guid Id,
        GameStatus Status,
        DateTimeOffset PlayedAtUtc,
        decimal MyBuyInAmount,
        decimal MyCashOutAmount,
        decimal MyNetAmount,
        int PlayerCount,
        decimal TotalBuyInAmount,
        decimal TotalCashOutAmount);
}
