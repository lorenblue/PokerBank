using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using PokerBank.Api.Data.Auth;

namespace PokerBank.Api.Features.Auth;

public static class Login
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", Handle)
            .WithName("Login")
            .WithTags("Auth")
            .WithSummary("Sign in.");

        return app;
    }

    private static async Task<Results<Ok<Response>, BadRequest<ErrorResponse>, UnauthorizedHttpResult>> Handle(
        Request request,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.BadRequest(new ErrorResponse("Email and password are required."));
        }

        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        return result.Succeeded
            ? TypedResults.Ok(new Response(request.Email))
            : TypedResults.Unauthorized();
    }

    private sealed record Request(string? Email, string? Password);

    private sealed record Response(string Email);
}
