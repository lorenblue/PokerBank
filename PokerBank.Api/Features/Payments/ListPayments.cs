using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Payments;

public static class ListPayments
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapListPayments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/payments", Handle)
            .WithName("ListPayments")
            .WithTags("Payments")
            .WithSummary("List payments.");

        return app;
    }

    private static async Task<Results<Ok<Response>, BadRequest<ErrorResponse>>> Handle(
        Guid? playerId,
        int? page,
        int? pageSize,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? DefaultPageSize;

        if (requestedPage < 1)
        {
            return TypedResults.BadRequest(new ErrorResponse("Page must be greater than or equal to 1."));
        }

        if (requestedPageSize is < 1 or > MaxPageSize)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Page size must be between 1 and {MaxPageSize}."));
        }

        var query = dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.PokerGroupId == groupContext.Id);

        if (playerId is not null)
        {
            query = query.Where(payment => payment.PlayerId == playerId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .Skip((requestedPage - 1) * requestedPageSize)
            .Take(requestedPageSize)
            .Select(payment => new PaymentResponse(
                payment.Id,
                payment.PlayerId,
                payment.Amount.Amount,
                payment.Direction,
                payment.Method,
                payment.RecordedAtUtc))
            .ToArrayAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / requestedPageSize);

        return TypedResults.Ok(new Response(
            payments,
            requestedPage,
            requestedPageSize,
            totalCount,
            totalPages));
    }

    private sealed record Response(
        PaymentResponse[] Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
