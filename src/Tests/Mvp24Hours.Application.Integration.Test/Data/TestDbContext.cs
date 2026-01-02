//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace Mvp24Hours.Application.Integration.Test.Data;

/// <summary>
/// Test database context for integration tests.
/// </summary>
public class TestDbContext : Mvp24HoursContext
{
    public TestDbContext() : base()
    {
    }

    public TestDbContext(DbContextOptions options) : base(options)
    {
    }

    public override bool CanApplyEntityLog => true;

    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId);
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(300);
        });
    }
}

