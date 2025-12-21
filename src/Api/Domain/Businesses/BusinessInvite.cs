using Api.Domain.Abstractions;

namespace Api.Domain.Businesses;

public sealed class BusinessInvite : AggregateRoot
{
    private BusinessInvite() { }

    public BusinessInvite(Guid id, Guid businessId, string invitedEmail, Guid invitedByUserId, BusinessMemberRole roleToGrant, string tokenHash, DateTimeOffset expiresAt, BusinessInviteStatus status, DateTimeOffset createdAt)
    {
        Id = id;
        BusinessId = businessId;
        InvitedEmail = invitedEmail;
        InvitedByUserId = invitedByUserId;
        RoleToGrant = roleToGrant;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string InvitedEmail { get; private set; } = null!;
    public Guid InvitedByUserId { get; private set; }
    public BusinessMemberRole RoleToGrant { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public BusinessInviteStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void MarkAccepted() => Status = BusinessInviteStatus.Accepted;

    public void MarkRevoked() => Status = BusinessInviteStatus.Revoked;

    public void MarkExpired() => Status = BusinessInviteStatus.Expired;
}
