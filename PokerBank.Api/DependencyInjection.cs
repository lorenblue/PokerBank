using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PokerBank.Api.Data;

namespace PokerBank.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICurrentPokerGroup, DefaultCurrentPokerGroup>();

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

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<PokerBankDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("PokerBank")
                    ?? throw new InvalidOperationException("Connection string 'PokerBank' is required.")));

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
