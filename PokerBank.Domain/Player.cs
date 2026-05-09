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

        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Player name is required.", nameof(name));
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Player name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        Id = id;
        Name = normalizedName;
    }

    public Guid Id { get; }

    public string Name { get; }
}
