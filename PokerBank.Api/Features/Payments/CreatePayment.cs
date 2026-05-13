using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> Handle(
        Request request,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players.AnyAsync(
            player => player.Id == request.PlayerId && player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Type) ||
            !Enum.TryParse<PaymentType>(request.Type, ignoreCase: true, out var type))
        {
            return TypedResults.BadRequest(new ErrorResponse("Payment type is invalid."));
        }

        var result = Payment.Record(request.PlayerId, new Money(request.Amount), type);

        if (result.IsFailed)
        {
            return Failure(result);
        }

        var payment = result.Value;

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/payments/{payment.Id}",
            new Response(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Type.ToString(),
                payment.RecordedAtUtc));
    }

    private static Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PaymentError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        return TypedResults.BadRequest(new ErrorResponse(message));
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
