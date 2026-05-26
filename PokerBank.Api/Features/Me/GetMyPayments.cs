using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

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

    private static async Task<Results<Ok<Response[]>, NotFound<ErrorResponse>>> Handle(
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

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.PokerGroupId == groupContext.Id &&
                payment.PlayerId == currentPlayer.Id)
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
