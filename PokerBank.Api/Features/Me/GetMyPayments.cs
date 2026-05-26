using Microsoft.AspNetCore.Http.HttpResults;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Payments;

namespace PokerBank.Api.Features.Me;

public static class GetMyPayments
{
    public static IEndpointRouteBuilder MapGetMyPayments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/payments", Handle)
            .WithName("GetMyPayments")
            .WithTags("Me")
            .WithSummary("Get my payments.");

        return app;
    }

    private static async Task<Results<Ok<PaymentResponse[]>, NotFound<ErrorResponse>>> Handle(
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

        var payments = await PaymentQuery.ListAsync(
            dbContext,
            groupContext.Id,
            currentPlayer.Id,
            cancellationToken);

        return TypedResults.Ok(payments
            .Select(PaymentResponse.From)
            .ToArray());
    }
}
