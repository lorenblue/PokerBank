namespace PokerBank.Api.Data;

public interface IPokerGroupContext
{
    Guid Id { get; }
}

public sealed class DefaultPokerGroupContext : IPokerGroupContext
{
    public Guid Id => DefaultPokerGroup.Id;
}
