namespace PokerBank.Api.Pagination;

public sealed record PagedResponse<T>(
    T[] Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
