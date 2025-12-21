namespace Api.Features.Auth;

public sealed record AuthResponse(Guid UserId, string Email, bool IsAdmin, string AccessToken, string RefreshToken);
public sealed record MeResponse(Guid UserId, string Email, bool IsAdmin);
