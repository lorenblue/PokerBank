using FluentResults;

namespace PokerBank.Domain;

public sealed class Payment
{
    private Payment()
    {
    }

    public static Result<Payment> Create(Guid playerId, Money amount, PaymentType type, PaymentMethod method)
    {
        if (playerId == Guid.Empty)
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPlayerId());
        }

        if (!amount.IsPositive)
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidAmount());
        }

        if (!Enum.IsDefined(type))
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPaymentType());
        }

        if (!Enum.IsDefined(method))
        {
            return Result.Fail<Payment>(PaymentErrors.InvalidPaymentMethod());
        }

        return Result.Ok(new Payment(Guid.NewGuid(), playerId, amount, type, method, DateTimeOffset.UtcNow));
    }

    private Payment(
        Guid id,
        Guid playerId,
        Money amount,
        PaymentType type,
        PaymentMethod method,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        PlayerId = playerId;
        Amount = amount;
        Type = type;
        Method = method;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PlayerId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentType Type { get; private set; }

    public PaymentMethod Method { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }
}
