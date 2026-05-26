using PokerBank.Domain;

namespace PokerBank.Api.Features.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid PlayerId,
    decimal Amount,
    PaymentDirection Direction,
    PaymentMethod Method,
    DateTimeOffset RecordedAtUtc)
{
    public static PaymentResponse From(PaymentRow row) =>
        new(
            row.Id,
            row.PlayerId,
            row.Amount,
            row.Direction,
            row.Method,
            row.RecordedAtUtc);

    public static PaymentResponse From(Payment payment) =>
        new(
            payment.Id,
            payment.PlayerId,
            payment.Amount.Amount,
            payment.Direction,
            payment.Method,
            payment.RecordedAtUtc);
}
