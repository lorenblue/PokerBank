using FluentResults;

namespace PokerBank.Domain;

public sealed class PaymentError(PaymentErrorCode code, string message) : Error(message)
{
    public PaymentErrorCode Code { get; } = code;
}
