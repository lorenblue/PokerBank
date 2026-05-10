namespace PokerBank.Domain;

public sealed class Player
{
    public const int MaxNameLength = 100;

    public Player(string name)
        : this(Guid.NewGuid(), name)
    {
    }

    internal Player(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Player id is required.", nameof(id));
        }

        Id = id;
        Name = NormalizeName(name);
        IsActive = true;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
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
}
