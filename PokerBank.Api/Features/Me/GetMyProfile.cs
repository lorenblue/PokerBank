using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Me;

public static class GetMyProfile
{
    public static IEndpointRouteBuilder MapGetMyProfile(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/profile", Handle)
            .WithName("GetMyProfile")
            .WithTags("Me")
            .WithSummary("Get my player profile.");

        return app;
    }

    private static async Task<Results<Ok<Response>, NotFound<ErrorResponse>>> Handle(
        ICurrentPlayerProvider currentPlayerProvider,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentPlayer = await currentPlayerProvider.GetAsync(cancellationToken);

        if (currentPlayer is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player profile was not found."));
        }

        var profile = await dbContext.Players
            .AsNoTracking()
            .Where(player =>
                player.Id == currentPlayer.Id &&
                player.PokerGroupId == groupContext.Id)
            .Select(player => new Response(
                player.Id,
                player.Name,
                player.EmailAddress,
                player.IsActive))
            .SingleAsync(cancellationToken);

        return TypedResults.Ok(profile);
    }

    private sealed record Response(
        Guid Id,
        string Name,
        string? EmailAddress,
        bool IsActive);
}
