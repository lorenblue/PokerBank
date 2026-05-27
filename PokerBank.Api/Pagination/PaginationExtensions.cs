using Microsoft.EntityFrameworkCore;

namespace PokerBank.Api.Pagination;

public static class PaginationExtensions
{
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToArrayAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / pageRequest.PageSize);

        return new PagedResponse<T>(
            items,
            pageRequest.Page,
            pageRequest.PageSize,
            totalCount,
            totalPages);
    }
}
