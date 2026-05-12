namespace PokerBank.Domain;

public static class PokerGameErrors
{
    public static PokerGameError InvalidPlayerId() => new(
        PokerGameErrorCode.InvalidPlayerId,
        "Player id is required.");

    public static PokerGameError InvalidAmount() => new(
        PokerGameErrorCode.InvalidAmount,
        "Amount must be positive.");

    public static PokerGameError GameClosed() => new(
        PokerGameErrorCode.GameClosed,
        "Closed games cannot be modified.");

    public static PokerGameError CashOutsExceedBuyIns() => new(
        PokerGameErrorCode.CashOutsExceedBuyIns,
        "Cash-outs cannot exceed total buy-ins.");

    public static PokerGameError BuyInsMustEqualCashOuts() => new(
        PokerGameErrorCode.BuyInsMustEqualCashOuts,
        "Cannot close a game until total buy-ins equal total cash-outs.");
}
