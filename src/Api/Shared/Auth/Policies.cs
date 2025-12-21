namespace Api.Shared.Auth;

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string BusinessMember = "BusinessMember";
    public const string BusinessOwner = "BusinessOwner";
    public const string BusinessAdminOrOwner = "BusinessAdminOrOwner";
}
