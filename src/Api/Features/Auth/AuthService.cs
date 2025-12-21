using Api.Data;
using Api.Domain.Auth;
using Api.Domain.Users;
using Api.Shared.Security;
using Api.Shared.Results;
using Api.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.Auth;

public sealed class AuthService(
    AppDbContext db,
    PasswordHasher passwordHasher,
    TokenHasher tokenHasher,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
            return Result<AuthResponse>.Failure(new Error("auth.email_exists", "Email ya está registrado"));

        var now = DateTimeOffset.UtcNow;
        var user = new User(Guid.NewGuid(), request.Email, passwordHasher.Hash(request.Password), false, now);
        db.Users.Add(user);

        var refresh = CreateRefreshToken(user.Id, now);
        db.RefreshTokens.Add(refresh.Entity);

        await db.SaveChangesAsync(ct);

        var access = jwtTokenService.GenerateAccessToken(user);
        var response = new AuthResponse(user.Id, user.Email, user.IsAdmin, access, refresh.PlainToken);
        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null)
            return Result<AuthResponse>.Failure(new Error("auth.invalid_credentials", "Credenciales inválidas"));

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(new Error("auth.invalid_credentials", "Credenciales inválidas"));

        var now = DateTimeOffset.UtcNow;
        var refresh = CreateRefreshToken(user.Id, now);
        db.RefreshTokens.Add(refresh.Entity);

        await db.SaveChangesAsync(ct);

        var access = jwtTokenService.GenerateAccessToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.IsAdmin, access, refresh.PlainToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokenHasher.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (token is null)
            return Result<AuthResponse>.Failure(new Error("auth.invalid_refresh", "Refresh token inválido"));

        if (token.IsExpired(DateTimeOffset.UtcNow))
            return Result<AuthResponse>.Failure(new Error("auth.expired_refresh", "Refresh token expirado"));

        var user = await db.Users.FindAsync(new object?[] { token.UserId }, cancellationToken: ct);
        if (user is null)
            return Result<AuthResponse>.Failure(new Error("auth.invalid_refresh", "Usuario no encontrado"));

        // rotate: revoke old, create new
        token.Revoke(DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var newRefresh = CreateRefreshToken(user.Id, now);
        db.RefreshTokens.Add(newRefresh.Entity);

        await db.SaveChangesAsync(ct);

        var access = jwtTokenService.GenerateAccessToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.IsAdmin, access, newRefresh.PlainToken));
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, Guid userId, CancellationToken ct)
    {
        var hash = tokenHasher.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);
        if (token is null)
            return Result.Failure(new Error("auth.invalid_refresh", "Refresh token inválido"));

        token.Revoke(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<MeResponse>> MeAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync(new object?[] { userId }, cancellationToken: ct);
        if (user is null)
            return Result<MeResponse>.Failure(new Error("auth.not_found", "Usuario no encontrado"));

        return Result<MeResponse>.Success(new MeResponse(user.Id, user.Email, user.IsAdmin));
    }

    private (RefreshToken Entity, string PlainToken) CreateRefreshToken(Guid userId, DateTimeOffset now)
    {
        var plain = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var hash = tokenHasher.Hash(plain);
        var entity = new RefreshToken(Guid.NewGuid(), userId, hash, now.AddDays(_jwt.RefreshTokenDays), now, null);
        return (entity, plain);
    }
}
