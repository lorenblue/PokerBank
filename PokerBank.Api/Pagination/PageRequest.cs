namespace PokerBank.Api.Pagination;

public sealed record PageRequest(int Page, int PageSize)
{
    private const int DefaultPage = 1;
    private const int MaxPageSize = 100;

    public static bool TryCreate(
        int? page,
        int? pageSize,
        int defaultPageSize,
        out PageRequest request,
        out ErrorResponse? error)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? defaultPageSize;

        if (requestedPage < 1)
        {
            request = new PageRequest(DefaultPage, defaultPageSize);
            error = new ErrorResponse("Page must be greater than or equal to 1.");
            return false;
        }

        if (requestedPageSize is < 1 or > MaxPageSize)
        {
            request = new PageRequest(DefaultPage, defaultPageSize);
            error = new ErrorResponse($"Page size must be between 1 and {MaxPageSize}.");
            return false;
        }

        request = new PageRequest(requestedPage, requestedPageSize);
        error = null;
        return true;
    }
}
