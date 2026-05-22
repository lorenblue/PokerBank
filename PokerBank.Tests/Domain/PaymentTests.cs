using PokerBank.Domain;

namespace PokerBank.Tests.Domain;

public sealed class PaymentTests
{
    [Theory]
    [InlineData(PaymentDirection.MadeByPlayer)]
    [InlineData(PaymentDirection.ReceivedByPlayer)]
    public void Create_ReturnsPayment(PaymentDirection direction)
    {
        var playerId = Guid.NewGuid();
        var amount = new Money(40m);
        const PaymentMethod method = PaymentMethod.ETransfer;

        var result = Payment.Create(playerId, amount, direction, method);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(playerId, result.Value.PlayerId);
        Assert.Equal(amount, result.Value.Amount);
        Assert.Equal(direction, result.Value.Direction);
        Assert.Equal(method, result.Value.Method);
        Assert.NotEqual(default, result.Value.RecordedAtUtc);
    }

    [Fact]
    public void Create_RequiresPlayerId()
    {
        var result = Payment.Create(
            Guid.Empty,
            new Money(40m),
            PaymentDirection.MadeByPlayer,
            PaymentMethod.ETransfer);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidPlayerId, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_RequiresPositiveAmount(decimal amount)
    {
        var result = Payment.Create(
            Guid.NewGuid(),
            new Money(amount),
            PaymentDirection.MadeByPlayer,
            PaymentMethod.ETransfer);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidAmount, error.Code);
    }

    [Fact]
    public void Create_RequiresValidPaymentDirection()
    {
        var result = Payment.Create(Guid.NewGuid(), new Money(40m), (PaymentDirection)0, PaymentMethod.ETransfer);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidPaymentDirection, error.Code);
    }

    [Fact]
    public void Create_RequiresValidPaymentMethod()
    {
        var result = Payment.Create(Guid.NewGuid(), new Money(40m), PaymentDirection.MadeByPlayer, (PaymentMethod)0);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Errors.OfType<PaymentError>());
        Assert.Equal(PaymentErrorCode.InvalidPaymentMethod, error.Code);
    }
}
