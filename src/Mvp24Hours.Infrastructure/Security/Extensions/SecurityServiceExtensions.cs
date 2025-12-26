//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Security.Providers;
using Mvp24Hours.Infrastructure.Security.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Mvp24Hours.Infrastructure.Security.Extensions
{
    /// <summary>
    /// Extension methods for registering security services.
    /// </summary>
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// Adds environment variable secret provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for environment variable options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the environment variable secret provider. Useful for development,
        /// testing, and containerized applications.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEnvironmentVariableSecretProvider(options =>
        /// {
        ///     options.VariableNamePrefix = "MYAPP_";
        ///     options.CaseSensitive = false;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddEnvironmentVariableSecretProvider(
            this IServiceCollection services,
            Action<EnvironmentVariableOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<EnvironmentVariableOptions>(_ => { });
            }

            services.AddSingleton<ISecretProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EnvironmentVariableOptions>>();
                return new EnvironmentVariableSecretProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Key Vault secret provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Azure Key Vault options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the Azure Key Vault secret provider. Requires the
        /// Azure.Security.KeyVault.Secrets NuGet package.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddAzureKeyVaultSecretProvider(options =>
        /// {
        ///     options.VaultUri = new Uri("https://myvault.vault.azure.net/");
        ///     options.UseManagedIdentity = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddAzureKeyVaultSecretProvider(
            this IServiceCollection services,
            Action<AzureKeyVaultOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);

            services.AddSingleton<ISecretProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureKeyVaultOptions>>();
                var logger = serviceProvider.GetService<ILogger<AzureKeyVaultSecretProvider>>();
                return new AzureKeyVaultSecretProvider(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds AWS Secrets Manager secret provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for AWS Secrets Manager options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the AWS Secrets Manager secret provider. Requires the
        /// AWSSDK.SecretsManager NuGet package.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddAwsSecretsManagerProvider(options =>
        /// {
        ///     options.Region = "us-east-1";
        ///     // IAM role will be used automatically if running on EC2/ECS/Lambda
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddAwsSecretsManagerProvider(
            this IServiceCollection services,
            Action<AwsSecretsManagerOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);

            services.AddSingleton<ISecretProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>();
                var logger = serviceProvider.GetService<ILogger<AwsSecretsManagerProvider>>();
                return new AwsSecretsManagerProvider(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds secret rotation helper to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the secret rotation helper. Requires <see cref="ISecretProvider"/>
        /// to be registered first.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSecretRotationHelper();
        /// 
        /// // Use in code
        /// var rotationHelper = serviceProvider.GetRequiredService&lt;ISecretRotationHelper&gt;();
        /// var needsRotation = await rotationHelper.NeedsRotationAsync("ApiKey", TimeSpan.FromDays(90), cancellationToken);
        /// </code>
        /// </example>
        public static IServiceCollection AddSecretRotationHelper(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<ISecretRotationHelper>(serviceProvider =>
            {
                var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
                var logger = serviceProvider.GetService<ILogger<SecretRotationHelper>>();
                return new SecretRotationHelper(secretProvider, logger);
            });

            return services;
        }
    }
}

