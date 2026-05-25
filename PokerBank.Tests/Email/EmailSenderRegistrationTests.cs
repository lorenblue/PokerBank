using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PokerBank.Api;
using PokerBank.Api.Email;

namespace PokerBank.Tests.Email;

public sealed class EmailSenderRegistrationTests
{
    [Fact]
    public void AddApiServices_UsesLoggingEmailSender_WhenSmtpIsDisabled()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.AddApiServices();

        using var services = builder.Services.BuildServiceProvider();

        var sender = services.GetRequiredService<IEmailSender>();

        Assert.IsType<LoggingEmailSender>(sender);
    }

    [Fact]
    public void AddApiServices_UsesMailKitEmailSender_WhenSmtpIsEnabled()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Smtp:Enabled"] = "true",
            ["Email:Smtp:Host"] = "smtp.example.com",
            ["Email:Smtp:Port"] = "587",
            ["Email:Smtp:FromEmail"] = "pokerbank@example.com",
            ["Email:Smtp:SecureSocketOptions"] = "StartTls"
        });
        builder.AddApiServices();

        using var services = builder.Services.BuildServiceProvider();

        var sender = services.GetRequiredService<IEmailSender>();

        Assert.IsType<MailKitEmailSender>(sender);
    }

    [Fact]
    public void AddApiServices_RejectsIncompleteSmtpConfiguration_WhenSmtpIsEnabled()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Smtp:Enabled"] = "true"
        });
        builder.AddApiServices();

        using var services = builder.Services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IEmailSender>());
    }
}
