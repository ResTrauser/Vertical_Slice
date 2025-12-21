using Api.Features.Plans;
using Api.Tests.Support;
using FluentAssertions;

namespace Api.Tests.Plans;

public class PlanServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsPlans()
    {
        using var db = TestDb.CreateDbContext();
        var p1 = db.AddPlan("Free", 1, 2, isSystem: true);
        var p2 = db.AddPlan("Pro", 3, 5);

        var service = new PlanService(db);

        var result = await service.GetAllAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value!.Select(x => x.Id).Should().Contain(new[] { p1.Id, p2.Id });
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        using var db = TestDb.CreateDbContext();
        var service = new PlanService(db);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Fails_WhenNameExists()
    {
        using var db = TestDb.CreateDbContext();
        db.AddPlan("Pro", 3, 5);
        var service = new PlanService(db);
        var request = new CreatePlanRequest("Pro", true, 3, 5);

        var result = await service.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("plans.name_exists");
    }

    [Fact]
    public async Task Update_Fails_WhenNameExistsOnOther()
    {
        using var db = TestDb.CreateDbContext();
        var p1 = db.AddPlan("Basic", 1, 2);
        db.AddPlan("Pro", 3, 5);
        var service = new PlanService(db);
        var request = new UpdatePlanRequest("Pro", true, 3, 5);

        var result = await service.UpdateAsync(p1.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("plans.name_exists");
    }

    [Fact]
    public async Task Delete_Fails_WhenSystemPlan()
    {
        using var db = TestDb.CreateDbContext();
        var sys = db.AddPlan("Free", 1, 2, isSystem: true);
        var service = new PlanService(db);

        var result = await service.DeleteAsync(sys.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("plans.system_protected");
    }
}
