//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Helpers;
using System;

namespace Mvp24Hours.Patterns.Test.Setup
{
    public static class Startup
    {
        public static IServiceProvider InitializeHttp()
        {
            var services = new ServiceCollection()
                            .AddSingleton(ConfigurationHelper.AppSettings);

            services.AddHttpClient("jsonUrl", client =>
            {
                client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            });

            services.AddHttpClient<HttpClientTest>(client =>
            {
                client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            });

            return services.BuildServiceProvider();
        }
    }
}
