//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.Basics;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Data
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

        #region [ Overrides ]
        public override bool CanApplyEntityLog => true;
        #endregion

        #region [ Sets ]

        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<Contact> Contact { get; set; }

        public virtual DbSet<CustomerBasic> CustomerBasic { get; set; }
        public virtual DbSet<ContactBasic> ContactBasic { get; set; }

        public virtual DbSet<CustomerBasicLog> CustomerLog { get; set; }
        public virtual DbSet<ContactBasicLog> ContactLog { get; set; }

        public virtual DbSet<CustomerBasicLog> CustomerBasicLog { get; set; }
        public virtual DbSet<ContactBasicLog> ContactBasicLog { get; set; }
        #endregion
    }
}
