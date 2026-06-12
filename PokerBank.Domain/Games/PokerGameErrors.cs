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

    public static PokerGameError PlayerHasNoBuyIns() => new(
        PokerGameErrorCode.PlayerHasNoBuyIns,
        "Player must have a buy-in before cashing out.");

    public static PokerGameError BuyInsMustEqualCashOuts() => new(
        PokerGameErrorCode.BuyInsMustEqualCashOuts,
        "Cannot close a game until total buy-ins equal total cash-outs.");

    public static PokerGameError EmptyGame() => new(
        PokerGameErrorCode.EmptyGame,
        "Cannot close a game with no activity.");

    public static PokerGameError EntryNotFound() => new(
        PokerGameErrorCode.EntryNotFound,
        "Game entry was not found.");

    public static PokerGameError EntryRecordedBeforeGameCreated() => new(
        PokerGameErrorCode.EntryRecordedBeforeGameCreated,
        "Game entries cannot be recorded before the game was created.");
}
