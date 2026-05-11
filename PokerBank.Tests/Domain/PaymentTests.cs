using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PaymentTests
{
    [Theory]
    [InlineData(PaymentType.PlayerPaysBank)]
    [InlineData(PaymentType.BankPaysPlayer)]
    public void NewPayment_RecordsPaymentDetails(PaymentType type)
    {
        var playerId = Guid.NewGuid();
        var amount = new Money(40m);

        var payment = new Payment(playerId, amount, type);

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(playerId, payment.PlayerId);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(type, payment.Type);
        Assert.NotEqual(default, payment.RecordedAtUtc);
    }

    [Fact]
    public void NewPayment_RequiresPlayerId()
    {
        Assert.Throws<ArgumentException>(() => new Payment(Guid.Empty, new Money(40m), PaymentType.PlayerPaysBank));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NewPayment_RequiresPositiveAmount(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Payment(Guid.NewGuid(), new Money(amount), PaymentType.PlayerPaysBank));
    }

    [Fact]
    public void NewPayment_RequiresValidPaymentType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Payment(Guid.NewGuid(), new Money(40m), (PaymentType)0));
    }
}
