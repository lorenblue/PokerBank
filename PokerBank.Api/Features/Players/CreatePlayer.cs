using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Players;

public static class CreatePlayer
{
    public static IEndpointRouteBuilder MapCreatePlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players", Handle)
            .WithName("CreatePlayer")
            .WithTags("Players")
            .WithSummary("Create a player.");

        return app;
    }

    private static async Task<Results<Created<Response>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Request request,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse("Player name is required."));
        }

        try
        {
            var player = new Player(request.Name);

            if (await dbContext.Players.AnyAsync(
                    existingPlayer => existingPlayer.IsActive && existingPlayer.Name == player.Name,
                    cancellationToken))
            {
                return TypedResults.Conflict(new ErrorResponse("An active player with this name already exists."));
            }

            dbContext.Players.Add(player);
            await dbContext.SaveChangesAsync(cancellationToken);

            return TypedResults.Created($"/players/{player.Id}", new Response(player.Id, player.Name, player.IsActive));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ErrorResponse(exception.Message));
        }
    }

    private sealed record Request(string? Name);

    private sealed record Response(Guid Id, string Name, bool IsActive);

    private sealed record ErrorResponse(string Error);
}
