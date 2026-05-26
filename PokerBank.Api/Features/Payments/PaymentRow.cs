using PokerBank.Domain;

namespace PokerBank.Api.Features.Payments;

public sealed record PaymentRow(
    Guid Id,
    Guid PlayerId,
    decimal Amount,
    PaymentDirection Direction,
    PaymentMethod Method,
    DateTimeOffset RecordedAtUtc);
