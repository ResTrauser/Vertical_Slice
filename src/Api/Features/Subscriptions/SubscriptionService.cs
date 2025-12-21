using Api.Data;
using Api.Domain.Businesses;
using Api.Domain.Subscriptions;
using Api.Shared.Auth;
using Api.Shared.Options;
using Api.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.Subscriptions;

public sealed class SubscriptionService(AppDbContext db, IOptions<SubscriptionOptions> options)
{
    private readonly SubscriptionOptions _options = options.Value;

    public async Task<Result<ActiveSubscriptionResponse>> ChangePlanAsync(Guid userId, ChangePlanRequest request, CancellationToken ct)
    {
        var newPlan = await db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);
        if (newPlan is null)
            return Result<ActiveSubscriptionResponse>.Failure(new Error("plans.not_found", "Plan no encontrado"));

        var active = await db.Subscriptions
            .OrderByDescending(s => s.StartAt)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active, ct);

        var now = DateTimeOffset.UtcNow;

        // If same plan already active, just return current state
        if (active is not null && active.PlanId == newPlan.Id)
        {
            var planName = newPlan.Name;
            return Result<ActiveSubscriptionResponse>.Success(
                new ActiveSubscriptionResponse(newPlan.Id, planName, SubscriptionStatus.Active, active.StartAt, active.EndAt));
        }

        if (active is not null)
        {
            active.End(now);
        }

        var downgradeMode = _options.DowngradeMode;

        // Enforce limits depending on mode
        var businesses = await db.Businesses
            .Include(b => b.Members)
            .Where(b => b.OwnerUserId == userId)
            .ToListAsync(ct);

        var activeBusinessCount = businesses.Count(b => b.IsActive);
        if (activeBusinessCount > newPlan.Limits.MaxBusinesses)
        {
            if (downgradeMode == DowngradeMode.Block)
            {
                return Result<ActiveSubscriptionResponse>.Failure(new Error("subscriptions.downgrade_blocked", "Excedes el límite de negocios para el nuevo plan"));
            }

            // Enforce: keep the oldest up to limit, deactivate the rest
            foreach (var b in businesses.OrderBy(b => b.CreatedAt).Skip(newPlan.Limits.MaxBusinesses))
            {
                b.Deactivate();
            }
        }

        // Validate/enforce members per business
        foreach (var business in businesses.Where(b => b.IsActive))
        {
            var activeMembers = business.Members.Where(m => m.IsActive).ToList();
            var maxMembers = newPlan.Limits.MaxMembersPerBusiness;

            if (activeMembers.Count > maxMembers)
            {
                if (downgradeMode == DowngradeMode.Block)
                {
                    return Result<ActiveSubscriptionResponse>.Failure(new Error("subscriptions.downgrade_blocked", "Excedes el límite de miembros por negocio para el nuevo plan"));
                }

                // Enforce: keep owner + más antiguos hasta el límite
                var ordered = activeMembers
                    .OrderBy(m => m.Role == BusinessMemberRole.Owner ? 0 : 1)
                    .ThenBy(m => m.JoinedAt)
                    .ToList();

                var keep = ordered.Take(maxMembers).ToHashSet();
                foreach (var member in activeMembers)
                {
                    if (!keep.Contains(member))
                    {
                        member.Deactivate();
                    }
                }
            }
        }

        var subscription = new Subscription(Guid.NewGuid(), userId, newPlan.Id, SubscriptionStatus.Active, now, null, "change_plan");
        db.Subscriptions.Add(subscription);

        await db.SaveChangesAsync(ct);

        return Result<ActiveSubscriptionResponse>.Success(
            new ActiveSubscriptionResponse(newPlan.Id, newPlan.Name, subscription.Status, subscription.StartAt, subscription.EndAt));
    }

    public async Task<Result<ActiveSubscriptionResponse>> GetActiveAsync(Guid userId, CancellationToken ct)
    {
        var active = await db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartAt)
            .Join(db.Plans, s => s.PlanId, p => p.Id, (s, p) => new { s, p })
            .FirstOrDefaultAsync(ct);

        if (active is null)
            return Result<ActiveSubscriptionResponse>.Failure(new Error("subscriptions.not_found", "No hay suscripción activa"));

        return Result<ActiveSubscriptionResponse>.Success(
            new ActiveSubscriptionResponse(active.p.Id, active.p.Name, active.s.Status, active.s.StartAt, active.s.EndAt));
    }

    public async Task<Result<List<SubscriptionHistoryItem>>> GetHistoryAsync(Guid userId, CancellationToken ct)
    {
        var history = await db.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartAt)
            .Join(db.Plans, s => s.PlanId, p => p.Id, (s, p) => new SubscriptionHistoryItem(p.Id, p.Name, s.Status, s.StartAt, s.EndAt))
            .ToListAsync(ct);

        return Result<List<SubscriptionHistoryItem>>.Success(history);
    }
}
