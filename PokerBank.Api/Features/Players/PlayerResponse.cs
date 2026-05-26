using PokerBank.Domain;

namespace PokerBank.Api.Features.Players;

public sealed record PlayerResponse(
    Guid Id,
    string Name,
    string? EmailAddress,
    bool IsActive)
{
    public static PlayerResponse From(Player player) =>
        new(player.Id, player.Name, player.EmailAddress, player.IsActive);
}
