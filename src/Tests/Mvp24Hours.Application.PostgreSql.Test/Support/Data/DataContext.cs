//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Application.PostgreSql.Test.Support.Entities;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace Mvp24Hours.Application.PostgreSql.Test.Support.Data
{
    public class DataContext : Mvp24HoursContext
    {
        #region [ Ctor ]

        public DataContext()
            : base()
        {
        }

        public DataContext(DbContextOptions options)
            : base(options)
        {
        }

        #endregion

        #region [ Sets ]

        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<Contact> Contact { get; set; }

        #endregion
    }
}
