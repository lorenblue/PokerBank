namespace PokerBank.Domain;

public static class PlayerInvitationErrors
{
    public static PlayerInvitationError InvalidPokerGroupId() => new(
        PlayerInvitationErrorCode.InvalidPokerGroupId,
        "Poker group id is required.");

    public static PlayerInvitationError InvalidPlayerId() => new(
        PlayerInvitationErrorCode.InvalidPlayerId,
        "Player id is required.");

    public static PlayerInvitationError InvalidEmailAddress() => new(
        PlayerInvitationErrorCode.InvalidEmailAddress,
        "Invitation email address is invalid.");

    public static PlayerInvitationError InvalidTokenHash() => new(
        PlayerInvitationErrorCode.InvalidTokenHash,
        "Invitation token hash is required.");

    public static PlayerInvitationError InvalidExpiration() => new(
        PlayerInvitationErrorCode.InvalidExpiration,
        "Invitation expiration must be in the future.");

    public static PlayerInvitationError Expired() => new(
        PlayerInvitationErrorCode.Expired,
        "Invitation has expired.");

    public static PlayerInvitationError AlreadyAccepted() => new(
        PlayerInvitationErrorCode.AlreadyAccepted,
        "Invitation has already been accepted.");
}
