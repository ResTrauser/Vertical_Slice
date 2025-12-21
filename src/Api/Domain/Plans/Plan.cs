using Api.Domain.Abstractions;

namespace Api.Domain.Plans;

public sealed class Plan : AggregateRoot
{
    private Plan() { }

    public Plan(Guid id, string name, bool isSystem, bool isActive, PlanLimits limits)
    {
        Id = id;
        Name = name;
        IsSystem = isSystem;
        IsActive = isActive;
        Limits = limits;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public PlanLimits Limits { get; private set; } = null!;

    public void Update(string name, bool isActive, PlanLimits limits)
    {
        Name = name;
        IsActive = isActive;
        Limits = limits;
    }
}
