using FluentResults;

namespace PokerBank.Domain;

public sealed class PokerEventError(PokerEventErrorCode code, string message) : Error(message)
{
    public PokerEventErrorCode Code { get; } = code;
}
