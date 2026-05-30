using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;

namespace PokerBank.Api.Features.Players;

public static class UpdatePlayer
{
    public static IEndpointRouteBuilder MapUpdatePlayer(this IEndpointRouteBuilder app)
    {
        app.MapPut("/players/{id:guid}", Handle)
            .WithName("UpdatePlayer")
            .WithTags("Players")
            .WithSummary("Update a player.");

        return app;
    }

    private static async Task<Results<Ok<PlayerResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Guid id,
        Request request,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse("Player name is required."));
        }

        var player = await dbContext.Players
            .SingleOrDefaultAsync(
                player => player.Id == id && player.PokerGroupId == groupContext.Id,
                cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        var normalizedName = request.Name.Trim();

        if (player.IsActive && await dbContext.Players.AnyAsync(
                existingPlayer =>
                    existingPlayer.IsActive &&
                    existingPlayer.PokerGroupId == groupContext.Id &&
                    existingPlayer.Id != player.Id &&
                    existingPlayer.Name == normalizedName,
                cancellationToken))
        {
            return TypedResults.Conflict(new ErrorResponse("An active player with this name already exists."));
        }

        try
        {
            player.Rename(request.Name);
            player.UpdateEmailAddress(request.EmailAddress);

            if (player is { IsActive: true, EmailAddress: not null } && await dbContext.Players.ActivePlayerEmailExistsAsync(
                    groupContext.Id,
                    player.EmailAddress,
                    excludingPlayerId: player.Id,
                    cancellationToken))
            {
                return TypedResults.Conflict(new ErrorResponse("An active player with this email address already exists."));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return TypedResults.Ok(PlayerResponse.From(player));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ErrorResponse(exception.Message));
        }
    }

    private sealed record Request(string? Name, string? EmailAddress);
}
