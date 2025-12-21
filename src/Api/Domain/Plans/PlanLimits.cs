namespace Api.Domain.Plans;

public sealed class PlanLimits
{
    private PlanLimits() { }

    public PlanLimits(int maxBusinesses, int maxMembersPerBusiness)
    {
        MaxBusinesses = maxBusinesses;
        MaxMembersPerBusiness = maxMembersPerBusiness;
    }

    public int MaxBusinesses { get; private set; }
    public int MaxMembersPerBusiness { get; private set; }
}
