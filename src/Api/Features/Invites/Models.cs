using Api.Domain.Businesses;

namespace Api.Features.Invites;

public sealed record CreateInviteRequest(Guid BusinessId, string InvitedEmail, BusinessMemberRole RoleToGrant);
public sealed record RevokeInviteRequest(Guid InviteId);
public sealed record AcceptInviteRequest(string Token);

public sealed record InviteResponse(Guid Id, Guid BusinessId, string InvitedEmail, BusinessMemberRole RoleToGrant, DateTimeOffset ExpiresAt, BusinessInviteStatus Status);
