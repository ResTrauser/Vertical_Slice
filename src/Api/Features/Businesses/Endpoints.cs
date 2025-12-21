using System.Security.Claims;
using Api.Shared.Auth;
using Microsoft.AspNetCore.Routing;

namespace Api.Features.Businesses;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/businesses");

        group.MapPost("/", async (CreateBusinessRequest request, BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.CreateAsync(userId, request, ct);
            return result.IsSuccess ? Results.Created($"/businesses/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapGet("/mine", async (BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.GetMineAsync(userId, ct);
            return Results.Ok(result.Value);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.GetByIdAsync(userId, id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/members", async (Guid id, AddMemberRequest request, BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.AddMemberAsync(userId, id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}/members/{memberUserId:guid}", async (Guid id, Guid memberUserId, BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.RemoveMemberAsync(userId, id, memberUserId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}/members/{memberUserId:guid}/role", async (Guid id, Guid memberUserId, ChangeMemberRoleRequest request, BusinessService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.ChangeMemberRoleAsync(userId, id, memberUserId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return app;
    }
}
