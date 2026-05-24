using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class GetPlayer
{
    public static IEndpointRouteBuilder MapGetPlayer(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players/{id:guid}", Handle)
            .WithName("GetPlayer")
            .WithTags("Players")
            .WithSummary("Get a player.");

        return app;
    }

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        ICurrentPokerGroup currentGroup,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Id == id && player.PokerGroupId == currentGroup.Id)
            .Select(player => new Response(player.Id, player.Name, player.EmailAddress, player.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return player is null
            ? TypedResults.NotFound(new ErrorResponse("Player was not found."))
            : TypedResults.Ok(player);
    }

    private sealed record Response(Guid Id, string Name, string? EmailAddress, bool IsActive);

}
