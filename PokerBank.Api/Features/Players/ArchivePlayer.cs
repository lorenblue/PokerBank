using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class ArchivePlayer
{
    public static IEndpointRouteBuilder MapArchivePlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/{id:guid}/archive", Handle)
            .WithName("ArchivePlayer")
            .WithTags("Players")
            .WithSummary("Archive a player.");

        return app;
    }

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        Guid id,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .SingleOrDefaultAsync(player => player.Id == id, cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        player.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(player.Id, player.Name, player.EmailAddress, player.IsActive));
    }

    private sealed record Response(Guid Id, string Name, string? EmailAddress, bool IsActive);

}
