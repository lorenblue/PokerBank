using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Games;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Balances;

public static class ListBalances
{
    public static IEndpointRouteBuilder MapListBalances(this IEndpointRouteBuilder app)
    {
        app.MapGet("/balances", Handle)
            .WithName("ListBalances")
            .WithTags("Balances")
            .WithSummary("List player balances.");

        return app;
    }

    private static async Task<Ok<Response[]>> Handle(
        Guid? playerId,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var gameNets = await dbContext.Games
            .AsNoTracking()
            .ToGameResults(playerId)
            .GroupBy(result => result.PlayerId)
            .Select(results => new PlayerAmount(
                results.Key,
                results.Sum(result => result.NetAmount)))
            .ToArrayAsync(cancellationToken);

        var paymentNets = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => playerId == null || payment.PlayerId == playerId)
            .GroupBy(payment => payment.PlayerId)
            .Select(payments => new PlayerAmount(
                payments.Key,
                payments.Sum(payment => payment.Type == PaymentType.BankPaysPlayer
                    ? payment.Amount.Amount
                    : -payment.Amount.Amount)))
            .ToArrayAsync(cancellationToken);

        var gameNetsByPlayerId = gameNets.ToDictionary(amount => amount.PlayerId, amount => amount.Amount);
        var paymentNetsByPlayerId = paymentNets.ToDictionary(amount => amount.PlayerId, amount => amount.Amount);

        var playerQuery = dbContext.Players.AsNoTracking();

        if (playerId is not null)
        {
            playerQuery = playerQuery.Where(player => player.Id == playerId);
        }

        var players = await playerQuery
            .OrderBy(player => player.Name)
            .Select(player => new PlayerProjection(player.Id, player.Name, player.IsActive))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(players
            .Select(player =>
            {
                var gameNetAmount = gameNetsByPlayerId.GetValueOrDefault(player.Id);
                var paymentNetAmount = paymentNetsByPlayerId.GetValueOrDefault(player.Id);

                return new Response(
                    player.Id,
                    player.Name,
                    player.IsActive,
                    gameNetAmount,
                    paymentNetAmount,
                    gameNetAmount - paymentNetAmount);
            })
            .ToArray());
    }

    private sealed record PlayerProjection(Guid Id, string Name, bool IsActive);

    private sealed record PlayerAmount(Guid PlayerId, decimal Amount);

    private sealed record Response(
        Guid PlayerId,
        string PlayerName,
        bool IsActive,
        decimal GameNetAmount,
        decimal PaymentNetAmount,
        decimal BalanceAmount);
}
