using Domain.Entities;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataBase.Configurations;

public class CompensationRequestConfiguration : IEntityTypeConfiguration<CompensationRequest>
{
    public void Configure(EntityTypeBuilder<CompensationRequest> builder)
    {
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.RequestedAmount).HasColumnType("decimal(18,2)");
        builder.Property(cr => cr.ApprovedAmount).HasColumnType("decimal(18,2)");
        builder.Property(cr => cr.IsDeleted).HasDefaultValue(false);
        builder.Property(cr => cr.Status)
            .HasConversion(
                v => v.ToString(), 
                s => Enum.Parse<CompensationStatus>(s))
            .HasMaxLength(20);
        
        builder.HasOne(cr => cr.Booking)
            .WithMany(b => b.CompensationRequests)
            .HasForeignKey(cr => cr.BookingId);
            
        builder.HasOne(cr => cr.Tenant)
            .WithMany(u => u.CompensationRequests)
            .HasForeignKey(cr => cr.TenantId);
            
        builder.HasOne(cr => cr.Property)
            .WithMany(p => p.CompensationRequests)
            .HasForeignKey(cr => cr.PropertyId);
    }
}