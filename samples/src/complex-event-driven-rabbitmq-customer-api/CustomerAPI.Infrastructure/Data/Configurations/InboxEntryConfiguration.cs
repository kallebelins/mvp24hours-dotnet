using CustomerAPI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerAPI.Infrastructure.Data.Configurations;

public class InboxEntryConfiguration : IEntityTypeConfiguration<InboxEntry>
{
    public void Configure(EntityTypeBuilder<InboxEntry> builder)
    {
        builder.ToTable("InboxEntries");
        builder.HasKey(x => x.MessageId);
        builder.Property(x => x.MessageType).IsRequired().HasMaxLength(300);
        builder.Property(x => x.ProcessedAt).IsRequired();
        builder.HasIndex(x => x.ProcessedAt);
    }
}
