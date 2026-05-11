using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class ArchivePlayer
{
    public static IEndpointRouteBuilder MapArchivePlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/{id:guid}/archive", Handle)
            .WithName("ArchivePlayer")
            .WithSummary("Archive a player.");

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .SingleOrDefaultAsync(player => player.Id == id, cancellationToken);

        if (player is null)
        {
            return Results.NotFound(new ErrorResponse("Player was not found."));
        }

        player.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new Response(player.Id, player.Name, player.IsActive));
    }

    private sealed record Response(Guid Id, string Name, bool IsActive);

    private sealed record ErrorResponse(string Error);
}
