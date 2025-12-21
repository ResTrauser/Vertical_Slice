using Api.Domain.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class BusinessMemberConfiguration : IEntityTypeConfiguration<BusinessMember>
{
    public void Configure(EntityTypeBuilder<BusinessMember> builder)
    {
        builder.ToTable("business_members");

        builder.HasKey(x => new { x.BusinessId, x.UserId });

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
    }
}
