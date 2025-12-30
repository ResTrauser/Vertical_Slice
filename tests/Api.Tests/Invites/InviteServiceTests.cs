using Api.Data;
using Api.Domain.Businesses;
using Api.Features.Businesses;
using Api.Features.Invites;
using Api.Shared.Email;
using Api.Shared.Options;
using Api.Shared.Security;
using Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Api.Tests.Invites;

public class InviteServiceTests
{
    private static InviteService CreateService(AppDbContext db, IDevEmailSender? emailSender = null, int expiryHours = 24)
    {
        var tokenHasher = new TokenHasher();
        emailSender ??= Substitute.For<IDevEmailSender>();
        var inviteOpts = Options.Create(new InviteOptions { InviteExpiryHours = expiryHours });
        return new InviteService(db, tokenHasher, emailSender, inviteOpts);
    }

    [Fact]
    public async Task Create_Fails_When_ActorNotOwnerOrAdmin()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var other = db.AddUser("other@test.com");
        var business = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(business);
        db.SaveChanges();

        var service = CreateService(db);
        var result = await service.CreateAsync(other.Id, new CreateInviteRequest(business.Id, "invitee@test.com", BusinessMemberRole.Member), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invites.forbidden");
    }

    [Fact]
    public async Task Accept_Fails_When_TokenInvalid()
    {
        using var db = TestDb.CreateDbContext();
        var user = db.AddUser("u@test.com");
        var service = CreateService(db);

        var result = await service.AcceptAsync(user.Id, new AcceptInviteRequest("invalid-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invites.invalid");
    }

    [Fact]
    public async Task Accept_Succeeds_AddsMember()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var invitee = db.AddUser("invitee@test.com");
        var business = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(business);
        db.SaveChanges();

        var tokenPlain = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var tokenHasher = new TokenHasher();
        var tokenHash = tokenHasher.Hash(tokenPlain);
        var invite = new BusinessInvite(Guid.NewGuid(), business.Id, invitee.Email, owner.Id, BusinessMemberRole.Member, tokenHash, DateTimeOffset.UtcNow.AddHours(1), BusinessInviteStatus.Pending, DateTimeOffset.UtcNow);
        db.BusinessInvites.Add(invite);
        db.SaveChanges();

        var service = CreateService(db);
        var result = await service.AcceptAsync(invitee.Id, new AcceptInviteRequest(tokenPlain), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.BusinessMembers.Any(m => m.BusinessId == business.Id && m.UserId == invitee.Id && m.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task Revoke_Fails_When_NotOwnerOrAdmin()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var actor = db.AddUser("actor@test.com");
        var business = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(business);
        var invite = new BusinessInvite(Guid.NewGuid(), business.Id, "invitee@test.com", owner.Id, BusinessMemberRole.Member, "hash", DateTimeOffset.UtcNow.AddHours(1), BusinessInviteStatus.Pending, DateTimeOffset.UtcNow);
        db.BusinessInvites.Add(invite);
        db.SaveChanges();

        var service = CreateService(db);
        var result = await service.RevokeAsync(actor.Id, invite.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invites.forbidden");
    }

    [Fact]
    public async Task Accept_Fails_When_Expired()
    {
        using var db = TestDb.CreateDbContext();
        var owner = db.AddUser("owner@test.com");
        var invitee = db.AddUser("invitee@test.com");
        var business = new Business(Guid.NewGuid(), owner.Id, "Biz", true, DateTimeOffset.UtcNow);
        db.Businesses.Add(business);
        db.SaveChanges();

        var tokenPlain = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var tokenHasher = new TokenHasher();
        var tokenHash = tokenHasher.Hash(tokenPlain);
        var invite = new BusinessInvite(Guid.NewGuid(), business.Id, invitee.Email, owner.Id, BusinessMemberRole.Member, tokenHash, DateTimeOffset.UtcNow.AddHours(-1), BusinessInviteStatus.Pending, DateTimeOffset.UtcNow.AddHours(-2));
        db.BusinessInvites.Add(invite);
        db.SaveChanges();

        var service = CreateService(db);
        var result = await service.AcceptAsync(invitee.Id, new AcceptInviteRequest(tokenPlain), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invites.expired");
    }
}
