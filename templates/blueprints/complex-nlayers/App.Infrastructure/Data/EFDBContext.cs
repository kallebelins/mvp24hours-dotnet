using System.Reflection;
using App.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace App.Infrastructure.Data;

public class EFDBContext : Mvp24HoursContext
{
    public EFDBContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public virtual DbSet<Item> Item { get; set; } = null!;
}
