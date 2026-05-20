namespace PokerBank.Domain;

public enum PokerGameErrorCode
{
    InvalidPlayerId = 1,
    InvalidAmount = 2,
    GameClosed = 3,
    CashOutsExceedBuyIns = 4,
    BuyInsMustEqualCashOuts = 5,
    PlayerHasNoBuyIns = 6,
    EntryNotFound = 7
}
