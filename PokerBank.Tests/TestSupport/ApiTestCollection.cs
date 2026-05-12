namespace PokerBank.Tests.TestSupport;

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<PokerBankApiFactory>
{
    public const string Name = "API tests";
}
