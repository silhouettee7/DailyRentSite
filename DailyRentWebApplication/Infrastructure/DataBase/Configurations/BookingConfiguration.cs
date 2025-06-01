using Domain.Entities;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataBase.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.AdultsCount).IsRequired();
        builder.Property(b => b.ChildrenCount).IsRequired();
        builder.Property(b => b.HasPets).HasDefaultValue(false);
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);
        builder.Property(b => b.Status)
            .HasConversion(
                v => v.ToString(), 
                s => Enum.Parse<BookingStatus>(s))
            .HasMaxLength(20);
        
        builder.HasOne(b => b.Property)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PropertyId);
            
        builder.HasOne(b => b.Tenant)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.TenantId);
    }
}