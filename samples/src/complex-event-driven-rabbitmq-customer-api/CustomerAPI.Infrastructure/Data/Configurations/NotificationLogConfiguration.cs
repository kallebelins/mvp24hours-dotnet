using CustomerAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerAPI.Infrastructure.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
    }
}
