using System.Security.Claims;
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Api.Shared.Auth;

public sealed record AdminOnlyRequirement : IAuthorizationRequirement;
public sealed record BusinessMemberRequirement : IAuthorizationRequirement;
public sealed record BusinessOwnerRequirement : IAuthorizationRequirement;
public sealed record BusinessAdminOrOwnerRequirement : IAuthorizationRequirement;

public sealed class AdminOnlyHandler : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOnlyRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public sealed class BusinessMemberHandler(AppDbContext db) : AuthorizationHandler<BusinessMemberRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BusinessMemberRequirement requirement)
    {
        if (!AuthorizationHelpers.TryGetUserAndBusiness(context, out var userId, out var businessId))
            return;

        var isMember = await db.BusinessMembers.AnyAsync(m => m.BusinessId == businessId && m.UserId == userId && m.IsActive);
        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class BusinessOwnerHandler(AppDbContext db) : AuthorizationHandler<BusinessOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BusinessOwnerRequirement requirement)
    {
        if (!AuthorizationHelpers.TryGetUserAndBusiness(context, out var userId, out var businessId))
            return;

        var isOwner = await db.Businesses.AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId);
        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class BusinessAdminOrOwnerHandler(AppDbContext db) : AuthorizationHandler<BusinessAdminOrOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BusinessAdminOrOwnerRequirement requirement)
    {
        if (!AuthorizationHelpers.TryGetUserAndBusiness(context, out var userId, out var businessId))
            return;

        var role = await db.BusinessMembers
            .Where(m => m.BusinessId == businessId && m.UserId == userId && m.IsActive)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (role is Domain.Businesses.BusinessMemberRole.Owner or Domain.Businesses.BusinessMemberRole.Admin)
        {
            context.Succeed(requirement);
        }
    }
}

internal static class AuthorizationHelpers
{
    public static bool TryGetUserAndBusiness(AuthorizationHandlerContext context, out Guid userId, out Guid businessId)
    {
        userId = Guid.Empty;
        businessId = Guid.Empty;

        if (context.User.GetUserId() is Guid u && u != Guid.Empty)
            userId = u;
        else
            return false;

        if (context.Resource is not HttpContext httpContext)
            return false;

        if (!httpContext.Request.RouteValues.TryGetValue("id", out var idObj))
            return false;

        return Guid.TryParse(idObj?.ToString(), out businessId);
    }
}
