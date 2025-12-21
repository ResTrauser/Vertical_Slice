using Api.Domain.Abstractions;

namespace Api.Domain.Businesses;

public sealed class Business : AggregateRoot
{
    private readonly List<BusinessMember> _members = new();

    private Business() { }

    public Business(Guid id, Guid ownerUserId, string name, bool isActive, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Name = name;
        IsActive = isActive;
        CreatedAt = createdAt;

        _members.Add(new BusinessMember(id, ownerUserId, BusinessMemberRole.Owner, true, createdAt));
    }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<BusinessMember> Members => _members;

    public void Rename(string name) => Name = name;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
