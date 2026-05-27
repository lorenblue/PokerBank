using PokerBank.Domain;

namespace PokerBank.Api.Features.Games;

public sealed record ListGamesResponse(Guid Id, GameStatus Status, DateTime CreatedAtUtc);
