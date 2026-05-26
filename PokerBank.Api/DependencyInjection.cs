using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PokerBank.Api.Auth;
using PokerBank.Api.Data;
using PokerBank.Api.Data.Auth;
using PokerBank.Api.Email;
using PokerBank.Api.Features.Me;
using PokerBank.Domain;

namespace PokerBank.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IPokerGroupContext, DefaultPokerGroupContext>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentPlayerProvider, CurrentPlayerProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, GroupRoleAuthorizationHandler>();
        builder.Services
            .AddOptions<SmtpEmailOptions>()
            .Bind(builder.Configuration.GetSection(SmtpEmailOptions.SectionName))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Host), "SMTP host is required when SMTP email is enabled.")
            .Validate(options => !options.Enabled || options.Port > 0, "SMTP port must be positive when SMTP email is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.FromEmail), "SMTP from email is required when SMTP email is enabled.")
            .Validate(options => !options.Enabled || options.HasCompleteCredentials, "SMTP username and password must both be configured, or both omitted.")
            .ValidateOnStart();

        builder.Services.AddSingleton<IEmailSender>(services =>
        {
            var smtpOptions = services.GetRequiredService<IOptions<SmtpEmailOptions>>().Value;

            return smtpOptions.Enabled
                ? ActivatorUtilities.CreateInstance<MailKitEmailSender>(services)
                : ActivatorUtilities.CreateInstance<LoggingEmailSender>(services);
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

        builder.Services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = jsonTypeInfo =>
            {
                var type = jsonTypeInfo.Type;

                if (type.DeclaringType is not null && type.Namespace?.StartsWith("PokerBank.Api.Features") is true)
                {
                    return $"{type.DeclaringType.Name}{type.Name}";
                }

                return OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
            };
        });

        builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "PokerBank.Auth";
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.ViewGroup, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new GroupRoleRequirement(GroupRole.Owner, GroupRole.Admin, GroupRole.Member)));

            options.AddPolicy(AuthorizationPolicies.ManageGroup, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new GroupRoleRequirement(GroupRole.Owner, GroupRole.Admin)));
        });

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<PokerBankDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("PokerBank")
                    ?? throw new InvalidOperationException("Connection string 'PokerBank' is required.")));

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<PokerBankDbContext>()
            .AddSignInManager();

        return builder;
    }

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
