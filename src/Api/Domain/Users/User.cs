using Api.Domain.Abstractions;

namespace Api.Domain.Users;

public sealed class User : AggregateRoot
{
    private User() { }

    public User(Guid id, string email, string passwordHash, bool isAdmin, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsAdmin { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void PromoteToAdmin() => IsAdmin = true;

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
