using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Data.Auth;

namespace PokerBank.Api.Features.Auth;

public static class GetCurrentUser
{
    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", Handle)
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .WithSummary("Get the signed-in user.")
            .RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal claimsPrincipal,
        UserManager<ApplicationUser> userManager,
        IPokerGroupContext groupContext,
        PokerBankDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);

        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var role = await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == user.Id && membership.PokerGroupId == groupContext.Id)
            .Select(membership => (PokerBank.Domain.GroupRole?)membership.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return TypedResults.Ok(new Response(user.Id, user.Email ?? user.UserName ?? string.Empty, groupContext.Id, role?.ToString()));
    }

    private sealed record Response(Guid Id, string Email, Guid PokerGroupId, string? GroupRole);
}
