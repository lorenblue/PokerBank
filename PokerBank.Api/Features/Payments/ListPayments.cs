using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Pagination;

namespace PokerBank.Api.Features.Payments;

public static class ListPayments
{
    private const int DefaultPageSize = 25;

    public static IEndpointRouteBuilder MapListPayments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/payments", Handle)
            .WithName("ListPayments")
            .WithTags("Payments")
            .WithSummary("List payments.");

        return app;
    }

    private static async Task<Results<Ok<PagedResponse<PaymentResponse>>, BadRequest<ErrorResponse>>> Handle(
        Guid? playerId,
        int? page,
        int? pageSize,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!PageRequest.TryCreate(page, pageSize, DefaultPageSize, out var pageRequest, out var error))
        {
            return TypedResults.BadRequest(error!);
        }

        var query = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PokerGroupId == groupContext.Id);

        if (playerId is not null)
        {
            query = query.Where(payment => payment.PlayerId == playerId);
        }

        var payments = query
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .Select(payment => new PaymentResponse(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Direction,
                payment.Method,
                payment.RecordedAtUtc));

        var response = await payments.ToPagedResponseAsync(pageRequest, cancellationToken);

        return TypedResults.Ok(response);
    }
}
