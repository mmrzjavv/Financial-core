using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Services.CoreService.Core.Domain.Identity.Entities;

namespace Services.CoreService.Core.Persistence.Configurations.Identity;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Company", DbSchemas.Identity);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.RegistrationNumber).HasMaxLength(64);
        builder.Property(e => e.PhoneNumber).HasMaxLength(32);
        builder.Property(e => e.Address).HasMaxLength(1024);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.RegistrationNumber);
    }
}
