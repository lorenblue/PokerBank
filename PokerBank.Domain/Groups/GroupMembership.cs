namespace PokerBank.Domain;

public sealed class GroupMembership
{
    private GroupMembership()
    {
    }

    public GroupMembership(Guid userId, Guid pokerGroupId, GroupRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (pokerGroupId == Guid.Empty)
        {
            throw new ArgumentException("Poker group id is required.", nameof(pokerGroupId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Group role is invalid.");
        }

        UserId = userId;
        PokerGroupId = pokerGroupId;
        Role = role;
    }

    public Guid UserId { get; private set; }

    public Guid PokerGroupId { get; private set; }

    public GroupRole Role { get; private set; }

    public bool CanManageGroup() => Role is GroupRole.Owner or GroupRole.Admin;
}
