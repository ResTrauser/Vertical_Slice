using Api.Data;
using Api.Domain.Plans;
using Api.Domain.Subscriptions;
using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Tests.Support;

public static class TestDb
{
    public static AppDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static User AddUser(this AppDbContext db, string email, bool isAdmin = false)
    {
        var user = new User(Guid.NewGuid(), email, "hash", isAdmin, DateTimeOffset.UtcNow);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Plan AddPlan(this AppDbContext db, string name, int maxBusinesses, int maxMembers, bool isSystem = false, bool isActive = true)
    {
        var plan = new Plan(Guid.NewGuid(), name, isSystem, isActive, new PlanLimits(maxBusinesses, maxMembers));
        db.Plans.Add(plan);
        db.SaveChanges();
        return plan;
    }

    public static Subscription AddActiveSubscription(this AppDbContext db, Guid userId, Guid planId)
    {
        var sub = new Subscription(Guid.NewGuid(), userId, planId, SubscriptionStatus.Active, DateTimeOffset.UtcNow, null, "seed");
        db.Subscriptions.Add(sub);
        db.SaveChanges();
        return sub;
    }
}
