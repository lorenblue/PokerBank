using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Domain;

namespace PokerBank.Api.Auth;

public sealed class GroupRoleRequirement(params GroupRole[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<GroupRole> AllowedRoles { get; } = allowedRoles;
}

public sealed class GroupRoleAuthorizationHandler(
    PokerBankDbContext dbContext,
    IPokerGroupContext groupContext) : AuthorizationHandler<GroupRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupRoleRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var hasMembership = await dbContext.GroupMemberships.AnyAsync(membership =>
            membership.UserId == userId &&
            membership.PokerGroupId == groupContext.Id &&
            requirement.AllowedRoles.Contains(membership.Role));

        if (hasMembership)
        {
            context.Succeed(requirement);
        }
    }
}
