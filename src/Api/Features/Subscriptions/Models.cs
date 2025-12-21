using Api.Domain.Subscriptions;

namespace Api.Features.Subscriptions;

public sealed record ChangePlanRequest(Guid PlanId);

public sealed record ActiveSubscriptionResponse(Guid PlanId, string PlanName, SubscriptionStatus Status, DateTimeOffset StartAt, DateTimeOffset? EndAt);

public sealed record SubscriptionHistoryItem(Guid PlanId, string PlanName, SubscriptionStatus Status, DateTimeOffset StartAt, DateTimeOffset? EndAt);
