using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class GetPlayer
{
    public static IEndpointRouteBuilder MapGetPlayer(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players/{id:guid}", Handle)
            .WithName("GetPlayer")
            .WithSummary("Get a player.");

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.Id == id)
            .Select(player => new Response(player.Id, player.Name, player.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return player is null
            ? Results.NotFound(new ErrorResponse("Player was not found."))
            : Results.Ok(player);
    }

    public sealed record Response(Guid Id, string Name, bool IsActive);

    public sealed record ErrorResponse(string Error);
}
