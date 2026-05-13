using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Payments;

public static class CreatePayment
{
    public static IEndpointRouteBuilder MapCreatePayment(this IEndpointRouteBuilder app)
    {
        app.MapPost("/payments", Handle)
            .WithName("CreatePayment")
            .WithTags("Payments")
            .WithSummary("Create a payment.");

        return app;
    }

    private static async Task<IResult> Handle(
        Request request,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players.AnyAsync(
            player => player.Id == request.PlayerId && player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return Results.NotFound(new ErrorResponse("Player was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Type) ||
            !Enum.TryParse<PaymentType>(request.Type, ignoreCase: true, out var type))
        {
            return Results.BadRequest(new ErrorResponse("Payment type is invalid."));
        }

        var result = Payment.Record(request.PlayerId, new Money(request.Amount), type);

        if (result.IsFailed)
        {
            return result.ToApiError();
        }

        var payment = result.Value;

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/payments/{payment.Id}",
            new Response(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Type.ToString(),
                payment.RecordedAtUtc));
    }

    private sealed record Request(Guid PlayerId, decimal Amount, string Type);

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record ErrorResponse(string Error);
}
