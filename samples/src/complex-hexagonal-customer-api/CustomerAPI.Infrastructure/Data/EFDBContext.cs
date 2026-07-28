using CustomerAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using System.Reflection;

namespace CustomerAPI.Infrastructure.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public class EFDBContext : Mvp24HoursContext
    {
        public EFDBContext()
            : base()
        {
        }

        public EFDBContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<Contact> Contact { get; set; }
    }
}
