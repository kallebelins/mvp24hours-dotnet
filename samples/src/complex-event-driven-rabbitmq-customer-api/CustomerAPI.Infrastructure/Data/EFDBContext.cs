using CustomerAPI.Domain.Entities;
using CustomerAPI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace CustomerAPI.Infrastructure.Data;

/// <summary>
/// Application EF Core context.
/// Includes:
///   - Customers        — business domain table
///   - NotificationLogs — consumer-side audit log (written on CustomerCreated processing)
///   - OutboxEntries    — durable outbox for integration events (written atomically with Customers)
///   - InboxEntries     — deduplication store for consumer idempotency
/// </summary>
public class EFDBContext(DbContextOptions<EFDBContext> options) : Mvp24HoursContext(options)
{
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
    public DbSet<OutboxEntry> OutboxEntries { get; set; } = null!;
    public DbSet<InboxEntry> InboxEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EFDBContext).Assembly);
    }
}
