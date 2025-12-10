using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    public static partial class DatabaseExtensions
    {
        #region [ Sync ]
        public static IHost MigrateDatabase<TContext>(this IHost host,
                                    Action<TContext, IServiceProvider> seeder,
                                    int? retry = 0) where TContext : Mvp24HoursContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-start", $"context:{typeof(TContext).Name}");
            int retryForAvailability = retry.Value;
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetService<TContext>();
                try
                {
                    InvokeSeeder(seeder, context, services);
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-invokeseeder");
                }
                catch (SqlException ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "infa-database-migrate-failure", ex);
                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        System.Threading.Thread.Sleep(2000);
                        host.MigrateDatabase(seeder, retryForAvailability);
                    }
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-end", $"context:{typeof(TContext).Name}");
                }
            }
            return host;
        }

        private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder,
                                                    TContext context,
                                                    IServiceProvider services)
                                                    where TContext : Mvp24HoursContext
        {
            context.Database.Migrate();
            seeder(context, services);
        }

        public static IHost MigrateDatabaseSQL<TContext>(this IHost host,
                                        Action<TContext, IServiceProvider> seeder,
                                        string[] commandStrings) where TContext : Mvp24HoursContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-start", $"context:{typeof(TContext).Name}");
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetService<TContext>();
                try
                {
                    int rowsAffected = InvokeSeederSQL(seeder, context, services, commandStrings);
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-invokeseeder", $"rows-affected:{rowsAffected}");
                }
                catch (SqlException ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "infa-database-migrate-sql-failure", ex);
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-end", $"context:{typeof(TContext).Name}");
                }
            }
            return host;
        }

        private static int InvokeSeederSQL<TContext>(Action<TContext, IServiceProvider> seeder,
                                            TContext context,
                                            IServiceProvider services,
                                            string[] commandStrings)
                                            where TContext : Mvp24HoursContext
        {
            int rowsAffected = 0;
            foreach (string commandString in commandStrings)
            {
                if (!string.IsNullOrWhiteSpace(commandString.Trim()))
                {
                    rowsAffected += context.Database.ExecuteSqlRaw(commandString);
                }
            }
            seeder(context, services);
            return rowsAffected;
        }

        public static string[] ReadSqlScriptFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("File script not found.");
            }
            string script = File.ReadAllText(fileName);
            return ReadSqlScriptFileRegex().Split(script);
        }
        #endregion

        #region [ Async ]
        public static async Task<IHost> MigrateDatabaseAsync<TContext>(this IHost host,
                            Action<TContext, IServiceProvider> seeder,
                            int? retry = 0) where TContext : Mvp24HoursContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-start", $"context:{typeof(TContext).Name}");
            int retryForAvailability = retry.Value;
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetService<TContext>();
                try
                {
                    await InvokeSeederAsync(seeder, context, services);
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-invokeseeder", $"context:{typeof(TContext).Name}");
                }
                catch (SqlException ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "infa-database-migrate-failure", ex, $"context:{typeof(TContext).Name}");
                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        System.Threading.Thread.Sleep(2000);
                        host.MigrateDatabase(seeder, retryForAvailability);
                    }
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-end", $"context:{typeof(TContext).Name}");
                }
            }
            return host;
        }

        private static async Task InvokeSeederAsync<TContext>(Action<TContext, IServiceProvider> seeder,
                                                    TContext context,
                                                    IServiceProvider services)
                                                    where TContext : Mvp24HoursContext
        {
            await context.Database.MigrateAsync();
            seeder(context, services);
        }

        public static async Task<IHost> MigrateDatabaseSQLAsync<TContext>(this IHost host,
                                        Action<TContext, IServiceProvider> seeder,
                                        string[] commandStrings) where TContext : Mvp24HoursContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-start", $"context:{typeof(TContext).Name}");
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetService<TContext>();
                try
                {
                    int rowsAffected = await InvokeSeederSQLAsync(seeder, context, services, commandStrings);
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-invokeseeder", $"context:{typeof(TContext).Name}|rows-affected:{rowsAffected}");
                }
                catch (SqlException ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "infa-database-migrate-sql-failure", ex, $"context:{typeof(TContext).Name}");
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information, "infa-database-migrate-sql-end", $"context:{typeof(TContext).Name}");
                }
            }
            return host;
        }

        private static async Task<int> InvokeSeederSQLAsync<TContext>(Action<TContext, IServiceProvider> seeder,
                                            TContext context,
                                            IServiceProvider services,
                                            string[] commandStrings)
                                            where TContext : Mvp24HoursContext
        {
            int rowsAffected = 0;
            foreach (string commandString in commandStrings)
            {
                if (!string.IsNullOrWhiteSpace(commandString.Trim()))
                {
                    rowsAffected += await context.Database.ExecuteSqlRawAsync(commandString);
                }
            }
            seeder(context, services);
            return rowsAffected;
        }


        public static async Task<string[]> ReadSqlScriptFileAsync(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("File script not found.");
            }
            string script = await File.ReadAllTextAsync(fileName);
            return ReadSqlScriptFileAsyncRegex().Split(script);
        }

        [GeneratedRegex(@"\bgo\b", RegexOptions.IgnoreCase | RegexOptions.Multiline, "pt-BR")]
        private static partial Regex ReadSqlScriptFileRegex();

        [GeneratedRegex(@"\bgo\b", RegexOptions.IgnoreCase | RegexOptions.Multiline, "pt-BR")]
        private static partial Regex ReadSqlScriptFileAsyncRegex();

        #endregion
    }
}
