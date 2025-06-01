using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataBase.Configurations;

public class RefreshSessionConfiguration: IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(s => s.User)
            .WithMany(u => u.RefreshSessions)
            .HasForeignKey(s => s.UserId);
        
        builder.Property(s => s.Fingerprint)
            .HasMinLength(10)
            .IsRequired();
    }
}