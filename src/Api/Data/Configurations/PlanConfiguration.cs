using Api.Data.Seeds;
using Api.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.IsSystem)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.OwnsOne(x => x.Limits, owned =>
        {
            owned.Property(p => p.MaxBusinesses)
                .HasColumnName("max_businesses")
                .IsRequired();

            owned.Property(p => p.MaxMembersPerBusiness)
                .HasColumnName("max_members_per_business")
                .IsRequired();
        });

        builder.Navigation(x => x.Limits).IsRequired();

        builder.HasData(
            new { Id = SystemPlans.FreeId, Name = "Free", IsSystem = true, IsActive = true },
            new { Id = SystemPlans.ProId, Name = "Pro", IsSystem = true, IsActive = true },
            new { Id = SystemPlans.BusinessId, Name = "Business", IsSystem = true, IsActive = true }
        );

        builder.OwnsOne(x => x.Limits).HasData(
            new { PlanId = SystemPlans.FreeId, MaxBusinesses = 1, MaxMembersPerBusiness = 2 },
            new { PlanId = SystemPlans.ProId, MaxBusinesses = 3, MaxMembersPerBusiness = 5 },
            new { PlanId = SystemPlans.BusinessId, MaxBusinesses = 10, MaxMembersPerBusiness = 20 }
        );
    }
}
