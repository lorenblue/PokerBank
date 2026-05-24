using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using PokerBank.Api.Data.Auth;

namespace PokerBank.Api.Features.Auth;

public static class Logout
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", Handle)
            .WithName("Logout")
            .WithTags("Auth")
            .WithSummary("Sign out.");

        return app;
    }

    private static async Task<Ok> Handle(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();

        return TypedResults.Ok();
    }
}
