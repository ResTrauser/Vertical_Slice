using System.Security.Claims;
using Api.Shared.Auth;
using Api.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService service, CancellationToken ct) =>
        {
            var result = await service.RegisterAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken ct) =>
        {
            var result = await service.LoginAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest request, AuthService service, CancellationToken ct) =>
        {
            var result = await service.RefreshAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequest request, AuthService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.LogoutAsync(request, userId, ct);
            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapGet("/me", async (AuthService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var result = await service.MeAsync(userId, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return app;
    }
}
