using CustomerAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using System.Reflection;

namespace CustomerAPI.Data
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Abbreviation for Entity Framework Database")]
    public class EFDBContext : Mvp24HoursContext
    {
        #region [ Ctor ]

        public EFDBContext()
            : base()
        {
        }

        public EFDBContext(DbContextOptions options)
            : base(options)
        {
        }
        /// <inheritdoc/>

        #endregion

        #region [ Overrides ]

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region [ Sets ]

        public virtual DbSet<Customer> Customer { get; set; }

        #endregion
    }
}
