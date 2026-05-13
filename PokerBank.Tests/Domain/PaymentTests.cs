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

    [Theory]
    [InlineData(PaymentType.PlayerPaysBank)]
    [InlineData(PaymentType.BankPaysPlayer)]
    public void Record_ReturnsPayment(PaymentType type)
    {
        var playerId = Guid.NewGuid();
        var amount = new Money(40m);

        var result = Payment.Record(playerId, amount, type);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(playerId, result.Value.PlayerId);
        Assert.Equal(amount, result.Value.Amount);
        Assert.Equal(type, result.Value.Type);
        Assert.NotEqual(default, result.Value.RecordedAtUtc);
    }

    [Fact]
    public void Record_RequiresPlayerId()
    {
        var result = Payment.Record(Guid.Empty, new Money(40m), PaymentType.PlayerPaysBank);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidPlayerId, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Record_RequiresPositiveAmount(decimal amount)
    {
        var result = Payment.Record(Guid.NewGuid(), new Money(amount), PaymentType.PlayerPaysBank);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidAmount, error.Code);
    }

    [Fact]
    public void Record_RequiresValidPaymentType()
    {
        var result = Payment.Record(Guid.NewGuid(), new Money(40m), (PaymentType)0);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidPaymentType, error.Code);
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
