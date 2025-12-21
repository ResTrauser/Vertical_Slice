using Api.Data;
using Api.Domain.Businesses;
using Api.Features.Businesses;
using Api.Shared.Email;
using Api.Shared.Options;
using Api.Shared.Results;
using Api.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Features.Invites;

public sealed class InviteService(
    AppDbContext db,
    TokenHasher tokenHasher,
    IDevEmailSender emailSender,
    IOptions<InviteOptions> inviteOptions)
{
    private readonly InviteOptions _inviteOptions = inviteOptions.Value;

    public async Task<Result<InviteResponse>> CreateAsync(Guid actorUserId, CreateInviteRequest request, CancellationToken ct)
    {
        var business = await db.Businesses.Include(b => b.Members).FirstOrDefaultAsync(b => b.Id == request.BusinessId, ct);
        if (business is null)
            return Result<InviteResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        var isOwnerOrAdmin = business.Members.Any(m => m.UserId == actorUserId && m.IsActive && (m.Role == BusinessMemberRole.Owner || m.Role == BusinessMemberRole.Admin));
        if (!isOwnerOrAdmin)
            return Result<InviteResponse>.Failure(new Error("invites.forbidden", "Solo owner o admin pueden invitar"));

        var tokenPlain = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var tokenHash = tokenHasher.Hash(tokenPlain);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(_inviteOptions.InviteExpiryHours);

        var invite = new BusinessInvite(Guid.NewGuid(), business.Id, request.InvitedEmail, actorUserId, request.RoleToGrant, tokenHash, expiresAt, BusinessInviteStatus.Pending, DateTimeOffset.UtcNow);
        db.BusinessInvites.Add(invite);
        await db.SaveChangesAsync(ct);

        await emailSender.SendAsync(request.InvitedEmail, "Invitación a negocio", $"Token: {tokenPlain}", ct);

        return Result<InviteResponse>.Success(ToResponse(invite));
    }

    public async Task<Result> RevokeAsync(Guid actorUserId, Guid inviteId, CancellationToken ct)
    {
        var invite = await db.BusinessInvites.FirstOrDefaultAsync(i => i.Id == inviteId, ct);
        if (invite is null)
            return Result.Failure(new Error("invites.not_found", "Invitación no encontrada"));

        var business = await db.Businesses.Include(b => b.Members).FirstOrDefaultAsync(b => b.Id == invite.BusinessId, ct);
        if (business is null)
            return Result.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        var isOwnerOrAdmin = business.Members.Any(m => m.UserId == actorUserId && m.IsActive && (m.Role == BusinessMemberRole.Owner || m.Role == BusinessMemberRole.Admin));
        if (!isOwnerOrAdmin)
            return Result.Failure(new Error("invites.forbidden", "Solo owner o admin pueden revocar"));

        if (invite.Status != BusinessInviteStatus.Pending)
            return Result.Failure(new Error("invites.invalid_status", "La invitación no está pendiente"));

        invite.MarkRevoked();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<BusinessResponse>> AcceptAsync(Guid userId, AcceptInviteRequest request, CancellationToken ct)
    {
        var tokenHash = tokenHasher.Hash(request.Token);
        var invite = await db.BusinessInvites.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);
        if (invite is null)
            return Result<BusinessResponse>.Failure(new Error("invites.invalid", "Invitación inválida"));

        if (invite.Status != BusinessInviteStatus.Pending)
            return Result<BusinessResponse>.Failure(new Error("invites.invalid_status", "Invitación no está pendiente"));

        if (invite.IsExpired(DateTimeOffset.UtcNow))
        {
            invite.MarkExpired();
            await db.SaveChangesAsync(ct);
            return Result<BusinessResponse>.Failure(new Error("invites.expired", "Invitación expirada"));
        }

        var business = await db.Businesses.Include(b => b.Members).FirstOrDefaultAsync(b => b.Id == invite.BusinessId, ct);
        if (business is null)
            return Result<BusinessResponse>.Failure(new Error("businesses.not_found", "Negocio no encontrado"));

        invite.MarkAccepted();

        var existing = business.Members.FirstOrDefault(m => m.UserId == userId);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            var member = new BusinessMember(business.Id, userId, invite.RoleToGrant, true, now);
            db.BusinessMembers.Add(member);
        }
        else
        {
            existing.SetRole(invite.RoleToGrant);
            existing.Activate();
        }

        await db.SaveChangesAsync(ct);
        return Result<BusinessResponse>.Success(new BusinessResponse(business.Id, business.Name, business.IsActive, business.CreatedAt, business.OwnerUserId,
            business.Members.Select(m => new BusinessMemberResponse(m.UserId, m.Role, m.IsActive, m.JoinedAt)).ToList()));
    }

    private static InviteResponse ToResponse(BusinessInvite invite) =>
        new(invite.Id, invite.BusinessId, invite.InvitedEmail, invite.RoleToGrant, invite.ExpiresAt, invite.Status);
}
