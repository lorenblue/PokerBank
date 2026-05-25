using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PokerBank.Api.Email;

public sealed class MailKitEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(smtp.FromName, smtp.FromEmail!));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;
        email.Body = new TextPart("plain")
        {
            Text = message.Body
        };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            smtp.Host!,
            smtp.Port,
            smtp.SecureSocketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.UserName))
        {
            await client.AuthenticateAsync(smtp.UserName, smtp.Password!, cancellationToken);
        }

        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
