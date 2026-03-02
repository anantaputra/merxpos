using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProcessedMessageConfiguration
    : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(x => x.MessageId)
               .IsUnique();

        builder.Property(x => x.ProcessedAt)
               .IsRequired();
    }
}