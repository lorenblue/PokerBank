using FluentResults;

namespace PokerBank.Domain;

public sealed class Payment
{
    private Payment()
    {
    }

    public static Result<Payment> Create(Guid playerId, Money amount, PaymentType type)
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

        return Result.Ok(new Payment(Guid.NewGuid(), playerId, amount, type, DateTimeOffset.UtcNow));
    }

    private Payment(Guid id, Guid playerId, Money amount, PaymentType type, DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Payment id is required.", nameof(id));
        }

        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(playerId));
        }

        if (!amount.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Payment type is invalid.");
        }

        Id = id;
        PlayerId = playerId;
        Amount = amount;
        Type = type;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PlayerId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentType Type { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }
}
