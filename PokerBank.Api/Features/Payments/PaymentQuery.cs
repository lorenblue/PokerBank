using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Payments;

public static class PaymentQuery
{
    public static async Task<PaymentRow[]> ListAsync(
        PokerBankDbContext dbContext,
        Guid pokerGroupId,
        Guid? playerId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PokerGroupId == pokerGroupId);

        if (playerId is not null)
        {
            query = query.Where(payment => payment.PlayerId == playerId);
        }

        return await query
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .Select(payment => new PaymentRow(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Direction,
                payment.Method,
                payment.RecordedAtUtc))
            .ToArrayAsync(cancellationToken);
    }
}
