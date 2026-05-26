using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Balances;

namespace PokerBank.Api.Features.Me;

public static class GetMyBalance
{
    public static IEndpointRouteBuilder MapGetMyBalance(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/balance", Handle)
            .WithName("GetMyBalance")
            .WithTags("Me")
            .WithSummary("Get my balance.");

        return app;
    }

    private static async Task<Results<Ok<BalanceResponse>, NotFound<ErrorResponse>>> Handle(
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

        var balance = await BalanceQuery.ListAsync(
            dbContext,
            groupContext.Id,
            currentPlayer.Id,
            activeOnly: false,
            cancellationToken);

        var myBalance = balance.Single();

        return TypedResults.Ok(BalanceResponse.From(myBalance));
    }
}
