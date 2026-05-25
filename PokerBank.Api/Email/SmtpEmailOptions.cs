using MailKit.Security;

namespace PokerBank.Api.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public bool Enabled { get; init; }

    public string? Host { get; init; }

    public int Port { get; init; } = 587;

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public string? FromEmail { get; init; }

    public string FromName { get; init; } = "PokerBank";

    public SecureSocketOptions SecureSocketOptions { get; init; } = SecureSocketOptions.StartTls;

    public bool HasCompleteCredentials =>
        string.IsNullOrWhiteSpace(UserName) == string.IsNullOrWhiteSpace(Password);
}
