using System.Net.Mail;

namespace PokerBank.Domain;

public sealed class Player
{
    public const int MaxNameLength = 100;
    public const int MaxEmailAddressLength = 254;

    public Player(string name, string? emailAddress = null)
        : this(Guid.NewGuid(), name, emailAddress)
    {
    }

    internal Player(Guid id, string name, string? emailAddress = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(id));
        }

        Id = id;
        Name = NormalizeName(name);
        EmailAddress = NormalizeEmailAddress(emailAddress);
        IsActive = true;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? EmailAddress { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void UpdateEmailAddress(string? emailAddress)
    {
        EmailAddress = NormalizeEmailAddress(emailAddress);
    }

    public void Archive()
    {
        IsActive = false;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name is required.", nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Player name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        return normalizedName;
    }

    private static string? NormalizeEmailAddress(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return null;
        }

        var normalizedEmailAddress = emailAddress.Trim();

        if (normalizedEmailAddress.Length > MaxEmailAddressLength)
        {
            throw new ArgumentException(
                $"Player email address cannot exceed {MaxEmailAddressLength} characters.");
        }

        try
        {
            var mailAddress = new MailAddress(normalizedEmailAddress);

            if (mailAddress.Address != normalizedEmailAddress || !mailAddress.Host.Contains('.'))
            {
                throw new ArgumentException("Player email address is invalid.");
            }
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Player email address is invalid.", exception);
        }

        return normalizedEmailAddress;
    }
}
