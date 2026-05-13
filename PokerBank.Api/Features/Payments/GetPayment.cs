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

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Id == id)
            .Select(payment => new Response(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Type.ToString(),
                payment.RecordedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return payment is null
            ? TypedResults.NotFound(new ErrorResponse("Payment was not found."))
            : TypedResults.Ok(payment);
    }

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record ErrorResponse(string Error);
}
