using CustomerAPI.Operations;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using NLog;

namespace CustomerAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection Configure(this IServiceCollection services, IConfiguration configuration)
        {
            #region [ Mvp24Hours ]
            services.AddMvp24HoursWebEssential();
            services.AddMvp24HoursWebJson();
            services.AddMvp24HoursWebSwagger("Customer Pipeline API");
            services.AddMvp24HoursWebGzip();
            #endregion

            services.AddMvp24HoursPipelineAsync();
            services.AddMyTelemetry();
            services.AddMyServices(configuration);
            services.AddMyHealthChecks(configuration);

            services.AddEndpointsApiExplorer();
            services.AddControllers();
            services.AddMvc();

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<GetCustomerClientStep>(sp =>
            {
                return new GetCustomerClientStep(configuration);
            });
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyTelemetry(this IServiceCollection services)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
#if DEBUG
            services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
                (name, state) =>
                {
                    if (name.EndsWith("-object"))
                    {
                        logger.Info($"{name}|body:{state.ToSerialize()}");
                    }
                    else
                    {
                        logger.Info($"{name}|{string.Join("|", state)}");
                    }
                }
            );
#endif
            services.AddMvp24HoursTelemetry(TelemetryLevels.Error,
                (name, state) =>
                {
                    if (name.EndsWith("-failure"))
                    {
                        logger.Error(state.ElementAtOrDefault(0) as Exception);
                    }
                    else
                    {
                        logger.Error($"{name}|{string.Join("|", state)}");
                    }
                }
            );
            return services;
        }
    }
}
