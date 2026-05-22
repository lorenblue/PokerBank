namespace PokerBank.Domain;

public static class PaymentErrors
{
    public static PaymentError InvalidPlayerId() => new(
        PaymentErrorCode.InvalidPlayerId,
        "Player id is required.");

    public static PaymentError InvalidAmount() => new(
        PaymentErrorCode.InvalidAmount,
        "Amount must be positive.");

    public static PaymentError InvalidPaymentDirection() => new(
        PaymentErrorCode.InvalidPaymentDirection,
        "Payment direction is invalid.");

    public static PaymentError InvalidPaymentMethod() => new(
        PaymentErrorCode.InvalidPaymentMethod,
        "Payment method is invalid.");
}
