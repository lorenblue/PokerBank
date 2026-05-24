using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

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

    private static async Task<Ok<Response[]>> Handle(
        Guid? playerId,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PokerGroupId == groupContext.Id);

        if (playerId is not null)
        {
            query = query.Where(payment => payment.PlayerId == playerId);
        }

        var payments = await query
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .Select(payment => new Response(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Direction,
                payment.Method,
                payment.RecordedAtUtc))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(payments);
    }

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        PaymentDirection Direction,
        PaymentMethod Method,
        DateTimeOffset RecordedAtUtc);
}
