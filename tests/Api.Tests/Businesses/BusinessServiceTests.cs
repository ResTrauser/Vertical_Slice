using Api.Data;
using Api.Domain.Businesses;
using Api.Domain.Plans;
using Api.Domain.Subscriptions;
using Api.Features.Businesses;
using Api.Shared.Options;
using Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Api.Tests.Businesses;

public class BusinessServiceTests
{
    private static BusinessService CreateService(AppDbContext db)
    {
        var opts = Options.Create(new SubscriptionOptions { DowngradeMode = DowngradeMode.Block });
        return new BusinessService(db, opts);
    }

    [Fact]
    public async Task Create_Fails_When_NoActiveSubscription()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var service = CreateService(db);

        var result = await service.CreateAsync(user.Id, new CreateBusinessRequest("Biz"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscriptions.not_found");
    }

    [Fact]
    public async Task Create_Fails_When_Exceeds_MaxBusinesses()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var plan = db.AddPlan("Limited", 1, 3);
        db.AddActiveSubscription(user.Id, plan.Id);
        db.Businesses.Add(new Business(Guid.NewGuid(), user.Id, "Existing", true, DateTimeOffset.UtcNow));
        db.SaveChanges();
        var service = CreateService(db);

        var result = await service.CreateAsync(user.Id, new CreateBusinessRequest("NewBiz"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("businesses.limit");
    }

    [Fact]
    public async Task AddMember_Fails_When_MaxMembersReached()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var member = db.AddUser("m1@test.com");
        var plan = db.AddPlan("LimitedMembers", 3, 1); // only owner allowed
        db.AddActiveSubscription(owner.Id, plan.Id);
        var biz = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(biz);
        db.SaveChanges();
        var service = CreateService(db);

        var result = await service.AddMemberAsync(owner.Id, biz.Id, new AddMemberRequest(member.Id, BusinessMemberRole.Member), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("members.limit");
    }
}
