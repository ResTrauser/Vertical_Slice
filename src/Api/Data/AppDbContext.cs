using Api.Domain.Auth;
using Api.Domain.Businesses;
using Api.Domain.Plans;
using Api.Domain.Subscriptions;
using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();
    public DbSet<BusinessInvite> BusinessInvites => Set<BusinessInvite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
