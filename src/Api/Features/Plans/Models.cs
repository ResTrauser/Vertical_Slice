namespace Api.Features.Plans;

public sealed record PlanResponse(Guid Id, string Name, bool IsSystem, bool IsActive, int MaxBusinesses, int MaxMembersPerBusiness);
public sealed record CreatePlanRequest(string Name, bool IsActive, int MaxBusinesses, int MaxMembersPerBusiness);
public sealed record UpdatePlanRequest(string Name, bool IsActive, int MaxBusinesses, int MaxMembersPerBusiness);
