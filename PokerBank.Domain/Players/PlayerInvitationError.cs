using FluentResults;

namespace PokerBank.Domain;

public sealed class PlayerInvitationError(PlayerInvitationErrorCode code, string message) : Error(message)
{
    public PlayerInvitationErrorCode Code { get; } = code;
}
