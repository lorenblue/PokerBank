namespace PokerBank.Domain;

public enum PaymentErrorCode
{
    InvalidPlayerId = 1,
    InvalidAmount = 2,
    InvalidPaymentDirection = 3,
    InvalidPaymentMethod = 4,
    InvalidPokerGroupId = 5
}
