using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Payments;

public static class ListPayments
{
    public static IEndpointRouteBuilder MapListPayments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/payments", Handle)
            .WithName("ListPayments")
            .WithTags("Payments")
            .WithSummary("List payments.");

        return app;
    }

    private static async Task<Ok<PaymentResponse[]>> Handle(
        Guid? playerId,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var payments = await PaymentQuery.ListAsync(
            dbContext,
            groupContext.Id,
            playerId,
            cancellationToken);

        return TypedResults.Ok(payments
            .Select(PaymentResponse.From)
            .ToArray());
    }
}
