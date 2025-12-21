using Api.Domain.Users;
using Api.Shared.Options;
using Api.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Data.Seeds;

public sealed class DatabaseSeeder(
    AppDbContext db,
    IOptions<AdminSeedOptions> adminOptions,
    PasswordHasher passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var options = adminOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
            return;

        var exists = await db.Users.AnyAsync(x => x.Email == options.Email, cancellationToken);
        if (exists)
            return;

        var now = DateTimeOffset.UtcNow;
        var user = new User(Guid.NewGuid(), options.Email, passwordHasher.Hash(options.Password), true, now);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
