using Api.Domain.Abstractions;

namespace Api.Domain.Subscriptions;

public sealed class Subscription : AggregateRoot
{
    private Subscription() { }

    public Subscription(Guid id, Guid userId, Guid planId, SubscriptionStatus status, DateTimeOffset startAt, DateTimeOffset? endAt, string changeReason)
    {
        Id = id;
        UserId = userId;
        PlanId = planId;
        Status = status;
        StartAt = startAt;
        EndAt = endAt;
        ChangeReason = changeReason;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset? EndAt { get; private set; }
    public string ChangeReason { get; private set; } = null!;

    public void End(DateTimeOffset endAt)
    {
        Status = SubscriptionStatus.Ended;
        EndAt = endAt;
    }
}
