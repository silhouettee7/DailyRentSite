using Domain.Entities;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataBase.Configurations;

public class PaymentConfiguration: IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder
            .HasKey(x => x.Id);
        
        builder.Property(p => p.CreatedAt)
            .HasDefaultValue(DateTime.UtcNow);
        
        builder.Property(p => p.Status)
            .HasConversion<string>(
                e => e.ToString(),
                s => Enum.Parse<PaymentStatus>(s));
    }
}