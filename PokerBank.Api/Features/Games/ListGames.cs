using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Pagination;

namespace PokerBank.Api.Features.Games;

public static class ListGames
{
    private const int DefaultPageSize = 25;

    public static IEndpointRouteBuilder MapListGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games", Handle)
            .WithName("ListGames")
            .WithTags("Games")
            .WithSummary("List games.");

        return app;
    }

    private static async Task<Results<Ok<PagedResponse<ListGamesResponse>>, BadRequest<ErrorResponse>>> Handle(
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

        var games = dbContext.Games
            .AsNoTracking()
            .Where(game => game.PokerGroupId == groupContext.Id)
            .OrderByDescending(game => game.CreatedAtUtc)
            .Select(game => new ListGamesResponse(game.Id, game.Status, game.CreatedAtUtc));

        var response = await games.ToPagedResponseAsync(pageRequest, cancellationToken);

        return TypedResults.Ok(response);
    }
}
