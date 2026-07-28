using Microsoft.EntityFrameworkCore;
using NotificationWorker.Entities;

namespace NotificationWorker.Data;

/// <summary>
/// EF Core context for the NotificationWorker's own data store.
/// Uses an in-memory database for this teaching sample.
/// </summary>
public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
        });
    }
}
