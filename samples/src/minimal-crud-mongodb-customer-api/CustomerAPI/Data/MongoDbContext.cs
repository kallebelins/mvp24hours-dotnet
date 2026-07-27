using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System.Reflection;

namespace CustomerAPI.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoDbContext : Mvp24HoursContext
    {
        #region [ Ctor ]

        public MongoDbContext(IOptions<MongoDbOptions> options)
            : base(options)
        {
            this.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        #endregion
    }
}
