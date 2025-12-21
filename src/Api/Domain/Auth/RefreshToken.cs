using Api.Domain.Abstractions;

namespace Api.Domain.Auth;

public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken() { }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, DateTimeOffset? revokedAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void Revoke(DateTimeOffset now) => RevokedAt = now;
}
