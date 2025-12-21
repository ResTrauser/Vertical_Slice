using Api.Domain.Businesses;

namespace Api.Features.Businesses;

public sealed record CreateBusinessRequest(string Name);
public sealed record BusinessResponse(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, Guid OwnerUserId, IReadOnlyList<BusinessMemberResponse> Members);
public sealed record BusinessMemberResponse(Guid UserId, BusinessMemberRole Role, bool IsActive, DateTimeOffset JoinedAt);

public sealed record AddMemberRequest(Guid UserId, BusinessMemberRole Role);
public sealed record ChangeMemberRoleRequest(BusinessMemberRole Role);
