using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data.Auth;
using PokerBank.Domain;

namespace PokerBank.Api.Data;

public static class DevelopmentAuthSeed
{
    public const string DefaultAdminEmail = "admin@pokerbank.local";
    public const string DefaultAdminPassword = "PokerBank123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var email = configuration["Authentication:AdminEmail"];
        var password = configuration["Authentication:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                return;
            }

            email = DefaultAdminEmail;
            password = DefaultAdminPassword;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<PokerBankDbContext>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create development admin user: {errors}");
            }
        }

        var hasMembership = await dbContext.GroupMemberships.AnyAsync(membership =>
            membership.UserId == user.Id &&
            membership.PokerGroupId == DefaultPokerGroup.Id);

        if (!hasMembership)
        {
            dbContext.GroupMemberships.Add(new GroupMembership(user.Id, DefaultPokerGroup.Id, GroupRole.Owner));
            await dbContext.SaveChangesAsync();
        }
    }
}
