using System.Security.Claims;
using Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Api.Features.Subscriptions;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions");

        group.MapPost("/change-plan", async (ChangePlanRequest request, SubscriptionService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.ChangePlanAsync(userId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapGet("/me/active", async (SubscriptionService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.GetActiveAsync(userId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization();

        group.MapGet("/me/history", async (SubscriptionService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.GetHistoryAsync(userId, ct);
            return Results.Ok(result.Value);
        }).RequireAuthorization();

        return app;
    }
}
