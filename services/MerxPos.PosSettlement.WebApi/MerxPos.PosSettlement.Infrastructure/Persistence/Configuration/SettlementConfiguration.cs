using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerxPos.PosSettlement.Domain.Entities;

namespace MerxPos.PosSettlement.Infrastructure.Persistence.Configurations;

public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // 🔥 VERY IMPORTANT → Idempotency protection
        builder.HasIndex(x => x.TransactionId)
            .IsUnique();
    }
}