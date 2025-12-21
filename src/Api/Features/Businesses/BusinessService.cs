using Api.Data;
using Api.Domain.Businesses;
using Api.Domain.Plans;
using Api.Domain.Subscriptions;
using Api.Shared.Auth;
using Api.Shared.Options;
using Api.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.Businesses;

public sealed class BusinessService(AppDbContext db, IOptions<SubscriptionOptions> subOptions)
{
    private readonly SubscriptionOptions _options = subOptions.Value;

    public async Task<Result<BusinessResponse>> CreateAsync(Guid userId, CreateBusinessRequest request, CancellationToken ct)
    {
        var planInfo = await GetActivePlanAsync(userId, ct);
        if (planInfo is null)
            return Result<BusinessResponse>.Failure(new Error("subscriptions.not_found", "No hay suscripción activa"));

        var limits = planInfo.Value.Limits;

        var activeCount = await db.Businesses.CountAsync(b => b.OwnerUserId == userId && b.IsActive, ct);
        if (activeCount >= limits.MaxBusinesses)
            return Result<BusinessResponse>.Failure(new Error("businesses.limit", "Has alcanzado el límite de negocios para tu plan"));

        var now = DateTimeOffset.UtcNow;
        var business = new Business(Guid.NewGuid(), userId, request.Name, isActive: true, createdAt: now);
        db.Businesses.Add(business);
        await db.SaveChangesAsync(ct);

        return Result<BusinessResponse>.Success(ToResponse(business));
    }

    public async Task<Result<List<BusinessResponse>>> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var businesses = await db.Businesses
            .Include(b => b.Members)
            .Where(b => b.OwnerUserId == userId)
            .ToListAsync(ct);

        return Result<List<BusinessResponse>>.Success(businesses.Select(ToResponse).ToList());
    }

    public async Task<Result<BusinessResponse>> GetByIdAsync(Guid userId, Guid businessId, CancellationToken ct)
    {
        var business = await db.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        if (business is null)
            return Result<BusinessResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        var isMember = business.Members.Any(m => m.UserId == userId && m.IsActive);
        if (!isMember)
            return Result<BusinessResponse>.Failure(new Error("businesses.forbidden", "No eres miembro de este negocio"));

        return Result<BusinessResponse>.Success(ToResponse(business));
    }

    public async Task<Result<BusinessResponse>> AddMemberAsync(Guid actorUserId, Guid businessId, AddMemberRequest request, CancellationToken ct)
    {
        var business = await db.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        if (business is null)
            return Result<BusinessResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        if (!IsOwner(actorUserId, business))
            return Result<BusinessResponse>.Failure(new Error("businesses.forbidden", "Solo el owner puede gestionar miembros"));

        var planInfo = await GetActivePlanAsync(business.OwnerUserId, ct);
        if (planInfo is null)
            return Result<BusinessResponse>.Failure(new Error("subscriptions.not_found", "No hay suscripción activa"));

        var limits = planInfo.Value.Limits;

        var activeMembers = business.Members.Count(m => m.IsActive);
        if (activeMembers >= limits.MaxMembersPerBusiness)
            return Result<BusinessResponse>.Failure(new Error("members.limit", "Límite de miembros para este negocio"));

        var existing = business.Members.FirstOrDefault(m => m.UserId == request.UserId);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            var member = new BusinessMember(business.Id, request.UserId, request.Role, true, now);
            db.BusinessMembers.Add(member);
        }
        else
        {
            existing.SetRole(request.Role);
            existing.Activate();
        }

        await db.SaveChangesAsync(ct);
        return Result<BusinessResponse>.Success(ToResponse(business));
    }

    public async Task<Result<BusinessResponse>> RemoveMemberAsync(Guid actorUserId, Guid businessId, Guid memberUserId, CancellationToken ct)
    {
        var business = await db.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        if (business is null)
            return Result<BusinessResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        if (!IsOwner(actorUserId, business))
            return Result<BusinessResponse>.Failure(new Error("businesses.forbidden", "Solo el owner puede gestionar miembros"));

        var member = business.Members.FirstOrDefault(m => m.UserId == memberUserId);
        if (member is null)
            return Result<BusinessResponse>.Failure(new Error("members.not_found", "Miembro no encontrado"));

        if (member.Role == BusinessMemberRole.Owner)
            return Result<BusinessResponse>.Failure(new Error("members.forbidden", "No puedes remover al owner"));

        member.Deactivate();
        await db.SaveChangesAsync(ct);
        return Result<BusinessResponse>.Success(ToResponse(business));
    }

    public async Task<Result<BusinessResponse>> ChangeMemberRoleAsync(Guid actorUserId, Guid businessId, Guid memberUserId, ChangeMemberRoleRequest request, CancellationToken ct)
    {
        var business = await db.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        if (business is null)
            return Result<BusinessResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        if (!IsOwner(actorUserId, business))
            return Result<BusinessResponse>.Failure(new Error("businesses.forbidden", "Solo el owner puede gestionar miembros"));

        var member = business.Members.FirstOrDefault(m => m.UserId == memberUserId);
        if (member is null)
            return Result<BusinessResponse>.Failure(new Error("members.not_found", "Miembro no encontrado"));

        if (member.Role == BusinessMemberRole.Owner)
            return Result<BusinessResponse>.Failure(new Error("members.forbidden", "No puedes cambiar rol del owner"));

        member.SetRole(request.Role);
        await db.SaveChangesAsync(ct);
        return Result<BusinessResponse>.Success(ToResponse(business));
    }

    private static bool IsOwner(Guid actorUserId, Business business) => business.OwnerUserId == actorUserId;

    private static BusinessResponse ToResponse(Business business)
    {
        var members = business.Members
            .Select(m => new BusinessMemberResponse(m.UserId, m.Role, m.IsActive, m.JoinedAt))
            .ToList();
        return new BusinessResponse(business.Id, business.Name, business.IsActive, business.CreatedAt, business.OwnerUserId, members);
    }

    private async Task<(Guid PlanId, PlanLimits Limits)?> GetActivePlanAsync(Guid userId, CancellationToken ct)
    {
        var active = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartAt)
            .Join(db.Plans.AsNoTracking(), s => s.PlanId, p => p.Id, (s, p) => new { p.Id, p.Limits })
            .FirstOrDefaultAsync(ct);

        return active is null ? null : (active.Id, active.Limits);
    }
}
