using Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Api.Features.Plans;

public static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/plans");

        // públicos
        group.MapGet("/", async (PlanService service, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ct);
            return Results.Ok(result.Value);
        });

        group.MapGet("/{id:guid}", async (Guid id, PlanService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        // admin
        group.MapPost("/", async (CreatePlanRequest request, PlanService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/plans/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPut("/{id:guid}", async (Guid id, UpdatePlanRequest request, PlanService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapDelete("/{id:guid}", async (Guid id, PlanService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        return app;
    }
}
