namespace PokerBank.Api.Features.Me;

public interface ICurrentPlayerProvider
{
    Task<CurrentPlayer?> GetAsync(CancellationToken cancellationToken);
}
