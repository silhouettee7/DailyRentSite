using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataBase.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.City).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Address).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Country).HasMaxLength(100).IsRequired();
        builder.Property(l => l.District).HasMaxLength(100);
    }
}