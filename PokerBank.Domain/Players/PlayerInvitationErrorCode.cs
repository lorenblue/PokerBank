namespace PokerBank.Domain;

public enum PlayerInvitationErrorCode
{
    InvalidPokerGroupId = 1,
    InvalidPlayerId = 2,
    InvalidEmailAddress = 3,
    InvalidTokenHash = 4,
    InvalidExpiration = 5,
    Expired = 6,
    AlreadyAccepted = 7
}
