namespace PokerBank.Api.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email to {To}. Subject: {Subject}. Body: {Body}",
            message.To,
            message.Subject,
            message.Body);

        return Task.CompletedTask;
    }
}
