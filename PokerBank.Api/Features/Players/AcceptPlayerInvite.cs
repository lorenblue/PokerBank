using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Data.Auth;
using PokerBank.Domain;

namespace PokerBank.Api.Features.Players;

public static class AcceptPlayerInvite
{
    public static IEndpointRouteBuilder MapAcceptPlayerInvite(this IEndpointRouteBuilder app)
    {
        app.MapPost("/invites/accept", Handle)
            .WithName("AcceptPlayerInvite")
            .WithTags("Players")
            .WithSummary("Accept a player invite.");

        return app;
    }

    private static async Task<Results<Ok<Response>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>, Conflict<ErrorResponse>>> Handle(
        Request request,
        PokerBankDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.BadRequest(new ErrorResponse("Token and password are required."));
        }

        var hash = PlayerInvitationToken.Hash(request.Token);

        var invitation = await dbContext.PlayerInvitations
            .SingleOrDefaultAsync(invitation => invitation.TokenHash == hash, cancellationToken);

        if (invitation is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Invitation was not found."));
        }

        var now = timeProvider.GetUtcNow();
        var acceptResult = invitation.Accept(now);

        if (acceptResult.IsFailed)
        {
            return TypedResults.Conflict(new ErrorResponse("Invitation can no longer be accepted."));
        }

        var player = await dbContext.Players
            .SingleOrDefaultAsync(
                player => player.Id == invitation.PlayerId && player.PokerGroupId == invitation.PokerGroupId,
                cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Player was not found."));
        }

        if (!player.IsActive)
        {
            return TypedResults.Conflict(new ErrorResponse("Archived players cannot accept invitations."));
        }

        if (player.UserId is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("Player is already linked to a user account."));
        }

        if (!string.Equals(player.EmailAddress, invitation.EmailAddress, StringComparison.Ordinal))
        {
            return TypedResults.Conflict(new ErrorResponse("Invitation email no longer matches the player email address."));
        }

        if (await userManager.FindByEmailAsync(invitation.EmailAddress) is not null)
        {
            return TypedResults.Conflict(new ErrorResponse("A user account with this email address already exists."));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = invitation.EmailAddress,
            Email = invitation.EmailAddress,
            EmailConfirmed = true
        };

        var identityResult = await userManager.CreateAsync(user, request.Password);

        if (!identityResult.Succeeded)
        {
            return TypedResults.BadRequest(new ErrorResponse(IdentityErrorMessage(identityResult)));
        }

        player.LinkUser(user.Id);
        dbContext.GroupMemberships.Add(new GroupMembership(user.Id, invitation.PokerGroupId, GroupRole.Member));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await signInManager.SignInAsync(user, isPersistent: true);

        return TypedResults.Ok(new Response(user.Id, user.Email));
    }

    private static string IdentityErrorMessage(IdentityResult identityResult)
    {
        var message = string.Join(" ", identityResult.Errors.Select(error => error.Description));

        return string.IsNullOrWhiteSpace(message)
            ? "User account could not be created."
            : message;
    }

    private sealed record Request(string? Token, string? Password);

    private sealed record Response(Guid UserId, string Email);
}
