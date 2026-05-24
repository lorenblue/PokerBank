namespace PokerBank.Domain;

public sealed class PokerGroup
{
    public const int MaxNameLength = 100;

    private PokerGroup()
    {
    }

    public PokerGroup(string name)
        : this(Guid.NewGuid(), name)
    {
    }

    internal PokerGroup(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Poker group id is required.", nameof(id));
        }

        Id = id;
        Name = NormalizeName(name);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

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
            throw new ArgumentException("Poker group name is required.", nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Poker group name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        return normalizedName;
    }
}
