//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Mvp24Hours.Infrastructure.Sms.Extensions
{
    /// <summary>
    /// Extension methods for registering SMS services.
    /// </summary>
    public static class SmsServiceExtensions
    {
        /// <summary>
        /// Adds SMS service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for SMS options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the SMS service infrastructure with dependency injection.
        /// By default, it uses <see cref="InMemorySmsProvider"/> if no provider is specified.
        /// </para>
        /// <para>
        /// To use a different provider, call one of the specific extension methods:
        /// - <see cref="AddTwilioSmsService"/>
        /// - <see cref="AddAzureCommunicationSmsService"/>
        /// - <see cref="AddInMemorySmsService"/>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSmsService(options =>
        /// {
        ///     options.DefaultFrom = "+5511888888888";
        ///     options.DefaultCountryCode = "BR";
        ///     options.MaxMessageLength = 160;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddSmsService(
            this IServiceCollection services,
            Action<SmsOptions>? configure = null)
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
                services.Configure<SmsOptions>(_ => { });
            }

            // Register InMemorySmsProvider by default (for testing/development)
            services.AddSingleton<ISmsService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SmsOptions>>().Value;
                return new InMemorySmsProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds SMS service with a custom provider factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory function to create the SMS service provider.</param>
        /// <param name="configure">Optional configuration action for SMS options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method allows registering a custom SMS service provider implementation.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSmsServiceWithProvider((serviceProvider, options) =>
        /// {
        ///     return new CustomSmsProvider(options);
        /// }, options =>
        /// {
        ///     options.DefaultFrom = "+5511888888888";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddSmsServiceWithProvider(
            this IServiceCollection services,
            Func<IServiceProvider, SmsOptions, ISmsService> factory,
            Action<SmsOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<SmsOptions>(_ => { });
            }

            services.AddSingleton<ISmsService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SmsOptions>>().Value;
                return factory(serviceProvider, options);
            });

            return services;
        }

        /// <summary>
        /// Adds Twilio SMS service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSms">Optional configuration action for SMS options.</param>
        /// <param name="configureTwilio">Configuration action for Twilio options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the Twilio SMS provider using Twilio REST API.
        /// </para>
        /// <para>
        /// <strong>Dependencies:</strong>
        /// Requires Twilio Account SID and Auth Token. Install Twilio package via:
        /// <c>dotnet add package Twilio</c>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTwilioSmsService(
        ///     smsOptions => smsOptions.DefaultFrom = "+5511888888888",
        ///     twilioOptions =>
        ///     {
        ///         twilioOptions.AccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        ///         twilioOptions.AuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddTwilioSmsService(
            this IServiceCollection services,
            Action<TwilioSmsOptions> configureTwilio,
            Action<SmsOptions>? configureSms = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureTwilio == null)
            {
                throw new ArgumentNullException(nameof(configureTwilio));
            }

            if (configureSms != null)
            {
                services.Configure(configureSms);
            }
            else
            {
                services.Configure<SmsOptions>(_ => { });
            }

            services.Configure(configureTwilio);

            // Ensure HttpClientFactory is registered
            services.AddHttpClient();

            services.AddSingleton<ISmsService>(serviceProvider =>
            {
                var smsOptions = serviceProvider.GetRequiredService<IOptions<SmsOptions>>();
                var twilioOptions = serviceProvider.GetRequiredService<IOptions<TwilioSmsOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetService<ILogger<TwilioSmsProvider>>();
                return new TwilioSmsProvider(smsOptions, twilioOptions, httpClientFactory, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Communication Services SMS service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSms">Optional configuration action for SMS options.</param>
        /// <param name="configureAzure">Configuration action for Azure Communication Services options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the Azure Communication Services SMS provider.
        /// Requires Azure Communication Services connection string.
        /// </para>
        /// <para>
        /// <strong>Dependencies:</strong>
        /// Requires Azure Communication Services connection string. Install Azure package via:
        /// <c>dotnet add package Azure.Communication.Sms</c>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddAzureCommunicationSmsService(
        ///     smsOptions => smsOptions.DefaultFrom = "+5511888888888",
        ///     azureOptions =>
        ///     {
        ///         azureOptions.ConnectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");
        ///         azureOptions.EnableDeliveryReports = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddAzureCommunicationSmsService(
            this IServiceCollection services,
            Action<AzureCommunicationSmsOptions> configureAzure,
            Action<SmsOptions>? configureSms = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureAzure == null)
            {
                throw new ArgumentNullException(nameof(configureAzure));
            }

            if (configureSms != null)
            {
                services.Configure(configureSms);
            }
            else
            {
                services.Configure<SmsOptions>(_ => { });
            }

            services.Configure(configureAzure);

            // Ensure HttpClientFactory is registered
            services.AddHttpClient();

            services.AddSingleton<ISmsService>(serviceProvider =>
            {
                var smsOptions = serviceProvider.GetRequiredService<IOptions<SmsOptions>>();
                var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureCommunicationSmsOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetService<ILogger<AzureCommunicationSmsProvider>>();
                return new AzureCommunicationSmsProvider(smsOptions, azureOptions, httpClientFactory, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds in-memory SMS service to the service collection (for testing/development).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for SMS options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the in-memory SMS provider which stores SMS messages in memory
        /// without actually sending them. Useful for testing and development.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddInMemorySmsService(options =>
        /// {
        ///     options.DefaultFrom = "+5511888888888";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddInMemorySmsService(
            this IServiceCollection services,
            Action<SmsOptions>? configure = null)
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
                services.Configure<SmsOptions>(_ => { });
            }

            services.AddSingleton<ISmsService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SmsOptions>>().Value;
                return new InMemorySmsProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds SMS template service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the in-memory SMS template service. For production use,
        /// consider implementing a persistent storage solution (e.g., database-backed template service).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSmsTemplateService();
        /// 
        /// // Use in code
        /// var templateService = serviceProvider.GetRequiredService&lt;ISmsTemplateService&gt;();
        /// var template = new SmsTemplate { Id = "welcome", Body = "Welcome {Name}!" };
        /// await templateService.SaveTemplateAsync(template);
        /// var rendered = await templateService.RenderByIdAsync("welcome", new Dictionary&lt;string, object&gt; { { "Name", "John" } });
        /// </code>
        /// </example>
        public static IServiceCollection AddSmsTemplateService(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<ISmsTemplateService, Services.InMemorySmsTemplateService>();
            return services;
        }

        /// <summary>
        /// Adds SMS rate limiter to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for rate limit options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the in-memory SMS rate limiter. For production use with multiple
        /// instances or distributed systems, consider implementing a distributed rate limiter using
        /// Redis or similar distributed cache.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSmsRateLimiter(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.MaxMessagesPerDestination = 10;
        ///     options.TimeWindow = TimeSpan.FromHours(1);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddSmsRateLimiter(
            this IServiceCollection services,
            Action<SmsRateLimitOptions>? configure = null)
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
                services.Configure<SmsRateLimitOptions>(_ => { });
            }

            services.AddSingleton<ISmsRateLimiter, Services.InMemorySmsRateLimiter>();
            return services;
        }
    }
}

