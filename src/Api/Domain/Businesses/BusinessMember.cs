namespace Api.Domain.Businesses;

public sealed class BusinessMember
{
    private BusinessMember() { }

    public BusinessMember(Guid businessId, Guid userId, BusinessMemberRole role, bool isActive, DateTimeOffset joinedAt)
    {
        BusinessId = businessId;
        UserId = userId;
        Role = role;
        IsActive = isActive;
        JoinedAt = joinedAt;
    }

    public Guid BusinessId { get; private set; }
    public Guid UserId { get; private set; }
    public BusinessMemberRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public void SetRole(BusinessMemberRole role) => Role = role;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
