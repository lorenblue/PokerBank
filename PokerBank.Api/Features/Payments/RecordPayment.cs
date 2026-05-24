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

    private static Task<Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> HandleMadeByPlayer(
        Guid playerId,
        Request request,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken) =>
        Handle(playerId, PaymentDirection.MadeByPlayer, request, currentGroup, dbContext, cancellationToken);

    private static Task<Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> HandleReceivedByPlayer(
        Guid playerId,
        Request request,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken) =>
        Handle(playerId, PaymentDirection.ReceivedByPlayer, request, currentGroup, dbContext, cancellationToken);

    private static async Task<Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> Handle(
        Guid playerId,
        PaymentDirection direction,
        Request request,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerExists = await dbContext.Players.AnyAsync(
            player =>
                player.Id == playerId &&
                player.PokerGroupId == currentGroup.Id &&
                player.IsActive,
            cancellationToken);

        if (!playerExists)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var result = Payment.Create(currentGroup.Id, playerId, new Money(request.Amount), direction, request.Method);

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
                payment.Direction,
                payment.Method,
                payment.RecordedAtUtc));
    }

    private static Results<Created<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>> Failure(ResultBase result)
    {
        var error = result.Errors.OfType<PaymentError>().FirstOrDefault();
        var message = error?.Message ?? result.Errors[0].Message;

        return TypedResults.BadRequest(new ErrorResponse(message));
    }

    private sealed record Request(decimal Amount, PaymentMethod Method);

    private sealed record Response(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        PaymentDirection Direction,
        PaymentMethod Method,
        DateTimeOffset RecordedAtUtc);
}
