using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Payments;

public static class RecordPayment
{
    public static IEndpointRouteBuilder MapRecordPayment(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/{playerId:guid}/payments/made", HandleMadeByPlayer)
            .WithName("RecordPaymentMadeByPlayer")
            .WithTags("Payments")
            .WithSummary("Record a payment made by a player.");

        app.MapPost("/players/{playerId:guid}/payments/received", HandleReceivedByPlayer)
            .WithName("RecordPaymentReceivedByPlayer")
            .WithTags("Payments")
            .WithSummary("Record a payment received by a player.");

        return app;
    }

    private static Task<Results<Created<PaymentResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> HandleMadeByPlayer(
        Guid playerId,
        Request request,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        Handle(playerId, PaymentDirection.MadeByPlayer, request, groupContext, dbContext, timeProvider, cancellationToken);

    private static Task<Results<Created<PaymentResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> HandleReceivedByPlayer(
        Guid playerId,
        Request request,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        Handle(playerId, PaymentDirection.ReceivedByPlayer, request, groupContext, dbContext, timeProvider, cancellationToken);

    private static async Task<Results<Created<PaymentResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> Handle(
        Guid playerId,
        PaymentDirection direction,
        Request request,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players.AnyAsync(
            player =>
                player.Id == playerId &&
                player.PokerGroupId == groupContext.Id &&
                player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var result = Payment.Create(
            groupContext.Id,
            playerId,
            new Money(request.Amount),
            direction,
            request.Method,
            timeProvider.GetUtcNow());

        if (result.IsFailed)
        {
            return Failure(result);
        }

        var payment = result.Value;

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/payments/{payment.Id}",
            PaymentResponse.From(payment));
    }

    private static Results<Created<PaymentResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PaymentError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        return TypedResults.BadRequest(new ErrorResponse(message));
    }

    private sealed record Request(decimal Amount, PaymentMethod Method);
}
