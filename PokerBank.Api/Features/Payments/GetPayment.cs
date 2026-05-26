using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Payments;

public static class GetPayment
{
    public static IEndpointRouteBuilder MapGetPayment(this IEndpointRouteBuilder app)
    {
        app.MapGet("/payments/{id:guid}", Handle)
            .WithName("GetPayment")
            .WithTags("Payments")
            .WithSummary("Get a payment.");

        return app;
    }

    private static async Task<Results<Ok<PaymentResponse>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Id == id && payment.PokerGroupId == groupContext.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return payment is null
            ? TypedResults.NotFound(new ErrorResponse("Payment was not found."))
            : TypedResults.Ok(PaymentResponse.From(payment));
    }
}
