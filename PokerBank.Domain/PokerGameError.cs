using FluentResults;

namespace PokerBank.Domain;

public sealed class PokerGameError(PokerGameErrorCode code, string message) : Error(message)
{
    public PokerGameErrorCode Code { get; } = code;
}
