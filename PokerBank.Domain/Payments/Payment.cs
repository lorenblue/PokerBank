using FluentResults;

namespace PokerBank.Domain;

public sealed class Payment
{
    private Payment()
    {
    }

    public static Result<Payment> Create(
        Guid pokerGroupId,
        Guid playerId,
        Money amount,
        PaymentDirection direction,
        PaymentMethod method,
        DateTimeOffset recordedAtUtc)
    {
        if (pokerGroupId == Guid.Empty)
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPokerGroupId());
        }

        if (playerId == Guid.Empty)
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPlayerId());
        }

        if (!amount.IsPositive)
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidAmount());
        }

        if (!Enum.IsDefined(direction))
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPaymentDirection());
        }

        if (!Enum.IsDefined(method))
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPaymentMethod());
        }

        return Result.Ok(new Payment(
            Guid.NewGuid(),
            pokerGroupId,
            playerId,
            amount,
            direction,
            method,
            recordedAtUtc));
    }

    private Payment(
        Guid id,
        Guid pokerGroupId,
        Guid playerId,
        Money amount,
        PaymentDirection direction,
        PaymentMethod method,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        PokerGroupId = pokerGroupId;
        PlayerId = playerId;
        Amount = amount;
        Direction = direction;
        Method = method;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PokerGroupId { get; private set; }

    public Guid PlayerId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentDirection Direction { get; private set; }

    public PaymentMethod Method { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }
}
