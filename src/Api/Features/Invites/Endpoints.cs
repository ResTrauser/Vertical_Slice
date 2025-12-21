using System.Security.Claims;
using Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Api.Features.Invites;

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invites");

        group.MapPost("/", async (CreateInviteRequest request, InviteService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.CreateAsync(userId, request, ct);
            return result.IsSuccess ? Results.Created($"/invites/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPost("/{inviteId:guid}/revoke", async (Guid inviteId, InviteService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.RevokeAsync(userId, inviteId, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPost("/accept", async (AcceptInviteRequest request, InviteService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.AcceptAsync(userId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return app;
    }
}
