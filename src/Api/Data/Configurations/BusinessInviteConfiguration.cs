using Api.Domain.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class BusinessInviteConfiguration : IEntityTypeConfiguration<BusinessInvite>
{
    public void Configure(EntityTypeBuilder<BusinessInvite> builder)
    {
        builder.ToTable("business_invites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.InvitedEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.InvitedByUserId)
            .IsRequired();

        builder.Property(x => x.RoleToGrant)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BusinessId);
        builder.HasIndex(x => x.TokenHash);
    }
}
