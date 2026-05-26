using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;

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

    private static async Task<Ok<BalanceResponse[]>> Handle(
        Guid? playerId,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var balances = await BalanceQuery.ListAsync(
            dbContext,
            groupContext.Id,
            playerId,
            activeOnly: false,
            cancellationToken);

        return TypedResults.Ok(balances
            .Select(BalanceResponse.From)
            .ToArray());
    }
}
