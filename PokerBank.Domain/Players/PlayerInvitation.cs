using System.Net.Mail;
using FluentResults;

namespace PokerBank.Domain;

public sealed class PlayerInvitation
{
    public const int MaxEmailAddressLength = 254;
    public const int MaxTokenHashLength = 512;

    private PlayerInvitation()
    {
    }

    public static Result<PlayerInvitation> Create(
        Guid pokerGroupId,
        Guid playerId,
        string? emailAddress,
        string? tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAtUtc)
    {
        if (pokerGroupId == Guid.Empty)
        {
            return Result.Fail<PlayerInvitation>(PlayerInvitationErrors.InvalidPokerGroupId());
        }

        if (playerId == Guid.Empty)
        {
            return Result.Fail<PlayerInvitation>(PlayerInvitationErrors.InvalidPlayerId());
        }

        var normalizedEmailAddress = NormalizeEmailAddress(emailAddress);

        if (normalizedEmailAddress is null)
        {
            return Result.Fail<PlayerInvitation>(PlayerInvitationErrors.InvalidEmailAddress());
        }

        var normalizedTokenHash = NormalizeTokenHash(tokenHash);

        if (normalizedTokenHash is null)
        {
            return Result.Fail<PlayerInvitation>(PlayerInvitationErrors.InvalidTokenHash());
        }

        if (expiresAtUtc <= now)
        {
            return Result.Fail<PlayerInvitation>(PlayerInvitationErrors.InvalidExpiration());
        }

        return Result.Ok(new PlayerInvitation(
            Guid.NewGuid(),
            pokerGroupId,
            playerId,
            normalizedEmailAddress,
            normalizedTokenHash,
            now,
            expiresAtUtc,
            acceptedAtUtc: null));
    }

    private PlayerInvitation(
        Guid id,
        Guid pokerGroupId,
        Guid playerId,
        string emailAddress,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc)
    {
        Id = id;
        PokerGroupId = pokerGroupId;
        PlayerId = playerId;
        EmailAddress = emailAddress;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PokerGroupId { get; private set; }

    public Guid PlayerId { get; private set; }

    public string EmailAddress { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public bool IsAccepted => AcceptedAtUtc is not null;

    public Result Accept(DateTimeOffset now)
    {
        if (IsAccepted)
        {
            return Result.Fail(PlayerInvitationErrors.AlreadyAccepted());
        }

        if (ExpiresAtUtc <= now)
        {
            return Result.Fail(PlayerInvitationErrors.Expired());
        }

        AcceptedAtUtc = now;

        return Result.Ok();
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
            return null;
        }

        try
        {
            var mailAddress = new MailAddress(normalizedEmailAddress);

            if (mailAddress.Address != normalizedEmailAddress || !mailAddress.Host.Contains('.'))
            {
                return null;
            }
        }
        catch (FormatException)
        {
            return null;
        }

        return normalizedEmailAddress.ToLowerInvariant();
    }

    private static string? NormalizeTokenHash(string? tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        var normalizedTokenHash = tokenHash.Trim();

        return normalizedTokenHash.Length > MaxTokenHashLength
            ? null
            : normalizedTokenHash;
    }
}
