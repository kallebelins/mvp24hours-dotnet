using CustomerAPI.Operations;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.Configuration;
using Microsoft.Extensions.Options;
using System;
using Mvp24Hours.Infrastructure.Http.Resilience;
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
            services.AddMvp24HoursNativeOpenApi(options =>
            {
                options.Title = "Customer Pipeline API";
                options.Version = "1.0.0";
                options.EnableSwaggerUI = true;
            });
            services.AddMvp24HoursWebGzip();
            #endregion



            services.AddMvp24HoursPipelineAsync(options =>
            {
                options.IsBreakOnFail = true;
            });
            services.AddMyOptions(configuration);
            services.AddMyServices();
            services.AddMyHealthChecks(configuration);



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
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            services.AddScoped<GetCustomerClientStep>();
            services.AddScoped<GetByCustomerMapperResponseStep>();
            services.AddScoped<GetByIdCustomerMapperResponseStep>();
            services.AddHttpClientWithStandardResilience(GetCustomerClientStep.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            return services;
        }
        /// <summary>
        /// Binds and validates external integration settings used by this host.
        /// </summary>
        public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptionsWithValidation<TypicodeOptions>(
                configuration.GetSection(TypicodeOptions.SectionName));
            return services;
        }





    }
}
