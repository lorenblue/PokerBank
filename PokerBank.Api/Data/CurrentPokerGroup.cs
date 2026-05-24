namespace PokerBank.Api.Data;

public interface ICurrentPokerGroup
{
    Guid Id { get; }
}

public sealed class DefaultCurrentPokerGroup : ICurrentPokerGroup
{
    public Guid Id => DefaultPokerGroup.Id;
}
