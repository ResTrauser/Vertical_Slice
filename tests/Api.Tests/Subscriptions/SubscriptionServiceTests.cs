using Api.Data;
using Api.Data.Seeds;
using Api.Domain.Businesses;
using Api.Domain.Plans;
using Api.Domain.Subscriptions;
using Api.Features.Subscriptions;
using Api.Shared.Options;
using Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Api.Tests.Subscriptions;

public class SubscriptionServiceTests
{
    private static SubscriptionService CreateService(AppDbContext db, DowngradeMode mode = DowngradeMode.Block)
    {
        var opts = Options.Create(new SubscriptionOptions { DowngradeMode = mode });
        return new SubscriptionService(db, opts);
    }

    [Fact]
    public async Task ChangePlan_Block_WhenExceedsBusinesses_ReturnsFailure()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var planOld = db.AddPlan("Pro", 3, 5);
        var planNew = db.AddPlan("Free", 1, 2);
        db.AddActiveSubscription(user.Id, planOld.Id);
        // two active businesses owned by user
        db.Businesses.Add(new Business(Guid.NewGuid(), user.Id, "B1", true, DateTimeOffset.UtcNow));
        db.Businesses.Add(new Business(Guid.NewGuid(), user.Id, "B2", true, DateTimeOffset.UtcNow.AddMinutes(1)));
        db.SaveChanges();

        var service = CreateService(db, DowngradeMode.Block);

        var result = await service.ChangePlanAsync(user.Id, new ChangePlanRequest(planNew.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscriptions.downgrade_blocked");
    }

    [Fact]
    public async Task ChangePlan_Enforce_DeactivatesExcessBusinesses()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var planOld = db.AddPlan("Pro", 3, 5);
        var planNew = db.AddPlan("Free", 1, 2);
        db.AddActiveSubscription(user.Id, planOld.Id);
        var b1 = new Business(Guid.NewGuid(), user.Id, "B1", true, DateTimeOffset.UtcNow.AddMinutes(-2));
        var b2 = new Business(Guid.NewGuid(), user.Id, "B2", true, DateTimeOffset.UtcNow.AddMinutes(-1));
        db.Businesses.AddRange(b1, b2);
        db.SaveChanges();

        var service = CreateService(db, DowngradeMode.Enforce);

        var result = await service.ChangePlanAsync(user.Id, new ChangePlanRequest(planNew.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Businesses.Count(b => b.IsActive).Should().Be(1);
    }

    [Fact]
    public async Task ChangePlan_Enforce_DeactivatesExtraMembers()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var member1 = db.AddUser("m1@test.com");
        var member2 = db.AddUser("m2@test.com");
        var planOld = db.AddPlan("Pro", 3, 5);
        var planNew = db.AddPlan("Free", 3, 1); // only 1 member per business
        db.AddActiveSubscription(owner.Id, planOld.Id);

        var b = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(b);
        db.SaveChanges();
        db.BusinessMembers.Add(new BusinessMember(b.Id, member1.Id, BusinessMemberRole.Member, true, DateTimeOffset.UtcNow.AddMinutes(-1)));
        db.BusinessMembers.Add(new BusinessMember(b.Id, member2.Id, BusinessMemberRole.Member, true, DateTimeOffset.UtcNow));
        db.SaveChanges();

        var service = CreateService(db, DowngradeMode.Enforce);

        var result = await service.ChangePlanAsync(owner.Id, new ChangePlanRequest(planNew.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.BusinessMembers.Count(m => m.BusinessId == b.Id && m.IsActive).Should().Be(1);
    }

    [Fact]
    public async Task GetActive_NotFound()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var service = CreateService(db);

        var result = await service.GetActiveAsync(user.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
