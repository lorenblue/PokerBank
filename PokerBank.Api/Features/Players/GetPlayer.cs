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

    private static async Task<Results<Ok<PlayerResponse>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Id == id && player.PokerGroupId == groupContext.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return player is null
            ? TypedResults.NotFound(new ErrorResponse("Player was not found."))
            : TypedResults.Ok(PlayerResponse.From(player));
    }
}
