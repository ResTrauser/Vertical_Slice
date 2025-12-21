using Api.Data;
using Api.Data.Seeds;
using Api.Domain.Plans;
using Api.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Plans;

public sealed class PlanService(AppDbContext db)
{
    public async Task<Result<List<PlanResponse>>> GetAllAsync(CancellationToken ct)
    {
        var items = await db.Plans
            .AsNoTracking()
            .Select(p => new PlanResponse(p.Id, p.Name, p.IsSystem, p.IsActive, p.Limits.MaxBusinesses, p.Limits.MaxMembersPerBusiness))
            .ToListAsync(ct);
        return Result<List<PlanResponse>>.Success(items);
    }

    public async Task<Result<PlanResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
            return Result<PlanResponse>.Failure(new Error("plans.not_found", "Plan no encontrado"));

        return Result<PlanResponse>.Success(new PlanResponse(plan.Id, plan.Name, plan.IsSystem, plan.IsActive, plan.Limits.MaxBusinesses, plan.Limits.MaxMembersPerBusiness));
    }

    public async Task<Result<PlanResponse>> CreateAsync(CreatePlanRequest request, CancellationToken ct)
    {
        var exists = await db.Plans.AnyAsync(p => p.Name == request.Name, ct);
        if (exists)
            return Result<PlanResponse>.Failure(new Error("plans.name_exists", "Ya existe un plan con ese nombre"));

        var plan = new Plan(Guid.NewGuid(), request.Name, isSystem: false, isActive: request.IsActive,
            limits: new PlanLimits(request.MaxBusinesses, request.MaxMembersPerBusiness));

        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);

        return Result<PlanResponse>.Success(new PlanResponse(plan.Id, plan.Name, plan.IsSystem, plan.IsActive, plan.Limits.MaxBusinesses, plan.Limits.MaxMembersPerBusiness));
    }

    public async Task<Result<PlanResponse>> UpdateAsync(Guid id, UpdatePlanRequest request, CancellationToken ct)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
            return Result<PlanResponse>.Failure(new Error("plans.not_found", "Plan no encontrado"));

        var nameExists = await db.Plans.AnyAsync(p => p.Id != id && p.Name == request.Name, ct);
        if (nameExists)
            return Result<PlanResponse>.Failure(new Error("plans.name_exists", "Ya existe un plan con ese nombre"));

        plan.Update(request.Name, request.IsActive, new PlanLimits(request.MaxBusinesses, request.MaxMembersPerBusiness));

        await db.SaveChangesAsync(ct);

        return Result<PlanResponse>.Success(new PlanResponse(plan.Id, plan.Name, plan.IsSystem, plan.IsActive, plan.Limits.MaxBusinesses, plan.Limits.MaxMembersPerBusiness));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
            return Result.Failure(new Error("plans.not_found", "Plan no encontrado"));

        if (plan.IsSystem)
            return Result.Failure(new Error("plans.system_protected", "No se puede eliminar un plan de sistema"));

        db.Plans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
