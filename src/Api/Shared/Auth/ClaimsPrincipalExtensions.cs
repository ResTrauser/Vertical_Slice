using System.Security.Claims;

namespace Api.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is not null && Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}
