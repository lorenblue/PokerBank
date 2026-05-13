namespace PokerBank.Domain;

public static class PaymentErrors
{
    public static PaymentError InvalidPlayerId() => new(
        PaymentErrorCode.InvalidPlayerId,
        "Player id is required.");

    public static PaymentError InvalidAmount() => new(
        PaymentErrorCode.InvalidAmount,
        "Amount must be positive.");

    public static PaymentError InvalidPaymentType() => new(
        PaymentErrorCode.InvalidPaymentType,
        "Payment type is invalid.");
}
