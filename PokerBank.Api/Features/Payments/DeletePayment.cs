using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Payments;

public static class DeletePayment
{
    public static IEndpointRouteBuilder MapDeletePayment(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/payments/{id:guid}", Handle)
            .WithName("DeletePayment")
            .WithTags("Payments")
            .WithSummary("Delete a payment.");

        return app;
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .Where(payment => payment.Id == id && payment.PokerGroupId == groupContext.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Payment was not found."));
        }

        dbContext.Payments.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

}
