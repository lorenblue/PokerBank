using Microsoft.EntityFrameworkCore;
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

    private static async Task<IResult> Handle(
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .Select(payment => new Response(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Type.ToString(),
                payment.RecordedAtUtc))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(payments);
    }

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);
}
