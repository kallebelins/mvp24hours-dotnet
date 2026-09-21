//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Pipe;

namespace Mvp24Hours.Application.Pipe.Test.Setup;

public static class StartupAsync
{
    // TODO (task 4.2c): ConfigurationHelper is obsolete. This setup has no host to bind options
    // from, so it keeps the static helper until the helper is removed in v12.
#pragma warning disable CS0618 // intentional: no host available in this static test setup
    private static IConfigurationRoot AppSettings => ConfigurationHelper.AppSettings;
#pragma warning restore CS0618

    public static IServiceProvider SetupInjectionAsync()
    {
        IServiceCollection services = new ServiceCollection()
                       .AddSingleton(AppSettings);

        services.AddMvp24HoursPipelineAsync(options => options.IsBreakOnFail = false);

        return services.BuildServiceProvider();
    }

    public static IServiceProvider SetupInjectionFactoryAsync()
    {
        IServiceCollection services = new ServiceCollection()
                       .AddSingleton(AppSettings);

        services.AddMvp24HoursPipelineAsync(factory: (sp) =>
        {
            var pipeline = new PipelineAsync(sp);
            pipeline.AddInterceptors(input =>
            {
                input.AddContent<int>("factory", 1);
                System.Diagnostics.Trace.WriteLine("Interceptor factory.");
            }, Core.Enums.Infrastructure.PipelineInterceptorType.PostOperation);
            return pipeline;
        });

        return services.BuildServiceProvider();
    }

}
