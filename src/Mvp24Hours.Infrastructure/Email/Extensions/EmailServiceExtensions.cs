//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Templates;
using Mvp24Hours.Infrastructure.Email.Queue;
using Mvp24Hours.Infrastructure.Email.RateLimiting;
using Mvp24Hours.Infrastructure.Email.Bulk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Email.Extensions
{
    /// <summary>
    /// Extension methods for registering email services.
    /// </summary>
    public static class EmailServiceExtensions
    {
        /// <summary>
        /// Adds email service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for email options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the email service infrastructure with dependency injection.
        /// By default, it uses <see cref="Providers.InMemoryEmailProvider"/> if no provider is specified.
        /// </para>
        /// <para>
        /// To use a different provider, call one of the specific extension methods:
        /// - <see cref="AddSmtpEmailService"/>
        /// - <see cref="AddSendGridEmailService"/> (requires SendGrid package)
        /// - <see cref="AddAzureCommunicationEmailService"/> (requires Azure.Communication.Email package)
        /// - <see cref="AddInMemoryEmailService"/>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEmailService(options =>
        /// {
        ///     options.DefaultFrom = "noreply@example.com";
        ///     options.DefaultReplyTo = "support@example.com";
        ///     options.MaxAttachmentSize = 10 * 1024 * 1024; // 10MB
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailService(
            this IServiceCollection services,
            Action<EmailOptions>? configure = null)
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
                services.Configure<EmailOptions>(_ => { });
            }

            // Register InMemoryEmailProvider by default (for testing/development)
            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
                return new Providers.InMemoryEmailProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds email service with a custom provider factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory function to create the email service provider.</param>
        /// <param name="configure">Optional configuration action for email options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method allows registering a custom email service provider implementation.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEmailServiceWithProvider((serviceProvider, options) =>
        /// {
        ///     return new CustomEmailProvider(options);
        /// }, options =>
        /// {
        ///     options.DefaultFrom = "noreply@example.com";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailServiceWithProvider(
            this IServiceCollection services,
            Func<IServiceProvider, EmailOptions, IEmailService> factory,
            Action<EmailOptions>? configure = null)
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
                services.Configure<EmailOptions>(_ => { });
            }

            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
                return factory(serviceProvider, options);
            });

            return services;
        }

        /// <summary>
        /// Adds SMTP email service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureEmail">Optional configuration action for email options.</param>
        /// <param name="configureSmtp">Configuration action for SMTP options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the SMTP email provider using System.Net.Mail.SmtpClient.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSmtpEmailService(
        ///     emailOptions => emailOptions.DefaultFrom = "noreply@example.com",
        ///     smtpOptions =>
        ///     {
        ///         smtpOptions.Host = "smtp.gmail.com";
        ///         smtpOptions.Port = 587;
        ///         smtpOptions.Username = "your-email@gmail.com";
        ///         smtpOptions.Password = "your-app-password";
        ///         smtpOptions.EnableStartTls = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddSmtpEmailService(
            this IServiceCollection services,
            Action<SmtpEmailOptions> configureSmtp,
            Action<EmailOptions>? configureEmail = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureSmtp == null)
            {
                throw new ArgumentNullException(nameof(configureSmtp));
            }

            if (configureEmail != null)
            {
                services.Configure(configureEmail);
            }
            else
            {
                services.Configure<EmailOptions>(_ => { });
            }

            services.Configure(configureSmtp);

            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var emailOptions = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();
                var smtpOptions = serviceProvider.GetRequiredService<IOptions<SmtpEmailOptions>>();
                var logger = serviceProvider.GetService<ILogger<SmtpEmailProvider>>();
                return new SmtpEmailProvider(emailOptions, smtpOptions, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds SendGrid email service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureEmail">Optional configuration action for email options.</param>
        /// <param name="configureSendGrid">Configuration action for SendGrid options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the SendGrid email provider using SendGrid Web API v3.
        /// Requires SendGrid API key.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddSendGridEmailService(
        ///     emailOptions => emailOptions.DefaultFrom = "noreply@example.com",
        ///     sendGridOptions =>
        ///     {
        ///         sendGridOptions.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        ///         sendGridOptions.EnableClickTracking = true;
        ///         sendGridOptions.EnableOpenTracking = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddSendGridEmailService(
            this IServiceCollection services,
            Action<SendGridEmailOptions> configureSendGrid,
            Action<EmailOptions>? configureEmail = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureSendGrid == null)
            {
                throw new ArgumentNullException(nameof(configureSendGrid));
            }

            if (configureEmail != null)
            {
                services.Configure(configureEmail);
            }
            else
            {
                services.Configure<EmailOptions>(_ => { });
            }

            services.Configure(configureSendGrid);

            // Ensure HttpClientFactory is registered
            services.AddHttpClient();

            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var emailOptions = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();
                var sendGridOptions = serviceProvider.GetRequiredService<IOptions<SendGridEmailOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetService<ILogger<SendGridEmailProvider>>();
                return new SendGridEmailProvider(emailOptions, sendGridOptions, httpClientFactory, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Communication Services email service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureEmail">Optional configuration action for email options.</param>
        /// <param name="configureAzure">Configuration action for Azure Communication Services options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the Azure Communication Services email provider.
        /// Requires Azure Communication Services connection string.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddAzureCommunicationEmailService(
        ///     emailOptions => emailOptions.DefaultFrom = "noreply@example.com",
        ///     azureOptions =>
        ///     {
        ///         azureOptions.ConnectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");
        ///         azureOptions.EnableUserEngagementTracking = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddAzureCommunicationEmailService(
            this IServiceCollection services,
            Action<AzureCommunicationEmailOptions> configureAzure,
            Action<EmailOptions>? configureEmail = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureAzure == null)
            {
                throw new ArgumentNullException(nameof(configureAzure));
            }

            if (configureEmail != null)
            {
                services.Configure(configureEmail);
            }
            else
            {
                services.Configure<EmailOptions>(_ => { });
            }

            services.Configure(configureAzure);

            // Ensure HttpClientFactory is registered
            services.AddHttpClient();

            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var emailOptions = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();
                var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureCommunicationEmailOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetService<ILogger<AzureCommunicationEmailProvider>>();
                return new AzureCommunicationEmailProvider(emailOptions, azureOptions, httpClientFactory, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds in-memory email service to the service collection (for testing/development).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for email options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the in-memory email provider which stores emails in memory
        /// without actually sending them. Useful for testing and development.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddInMemoryEmailService(options =>
        /// {
        ///     options.DefaultFrom = "test@example.com";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddInMemoryEmailService(
            this IServiceCollection services,
            Action<EmailOptions>? configure = null)
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
                services.Configure<EmailOptions>(_ => { });
            }

            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();
                return new InMemoryEmailProvider(options);
            });

            return services;
        }

        /// <summary>
        /// Adds email template renderer services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="templateEngine">The template engine to use (Scriban or Razor).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the email template renderer. By default, it uses Scriban
        /// (Liquid-like syntax). Use Razor for C# templates with complex logic.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use Scriban (Liquid-like syntax)
        /// services.AddEmailTemplateRenderer(TemplateEngine.Scriban);
        /// 
        /// // Use Razor (C# syntax)
        /// services.AddEmailTemplateRenderer(TemplateEngine.Razor);
        /// 
        /// // Use in code
        /// var renderer = serviceProvider.GetRequiredService&lt;IEmailTemplateRenderer&gt;();
        /// var html = await renderer.RenderAsync("Hello {{ name }}!", new { name = "John" });
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailTemplateRenderer(
            this IServiceCollection services,
            TemplateEngine templateEngine = TemplateEngine.Scriban)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            switch (templateEngine)
            {
                case TemplateEngine.Scriban:
                    services.AddSingleton<IEmailTemplateRenderer, ScribanEmailTemplateRenderer>();
                    break;
                case TemplateEngine.Razor:
                    services.AddSingleton<IEmailTemplateRenderer, RazorEmailTemplateRenderer>();
                    break;
                default:
                    throw new ArgumentException($"Unknown template engine: {templateEngine}", nameof(templateEngine));
            }

            return services;
        }

        /// <summary>
        /// Adds email queue services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useInMemory">Whether to use in-memory queue (default: true).</param>
        /// <param name="startProcessor">Whether to start the background queue processor (default: true).</param>
        /// <param name="configureProcessor">Optional configuration for the queue processor.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the email queue service. By default, it uses an in-memory queue
        /// suitable for testing and single-instance scenarios. For production distributed scenarios,
        /// consider implementing a persistent queue (Redis, RabbitMQ, etc.).
        /// </para>
        /// <para>
        /// The queue processor runs as a background service and automatically processes queued emails.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEmailQueue(
        ///     startProcessor: true,
        ///     configureProcessor: options =>
        ///     {
        ///         options.PollInterval = TimeSpan.FromSeconds(10);
        ///         options.MaxRetryAttempts = 5;
        ///     });
        /// 
        /// // Use in code
        /// var queue = serviceProvider.GetRequiredService&lt;IEmailQueue&gt;();
        /// var queueId = await queue.EnqueueAsync(emailMessage);
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailQueue(
            this IServiceCollection services,
            bool useInMemory = true,
            bool startProcessor = true,
            Action<EmailQueueProcessorOptions>? configureProcessor = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (useInMemory)
            {
                services.AddSingleton<IEmailQueue, InMemoryEmailQueue>();
            }

            if (startProcessor)
            {
                services.Configure(configureProcessor ?? (_ => { }));
                services.AddHostedService<EmailQueueProcessor>();
            }

            return services;
        }

        /// <summary>
        /// Adds email rate limiter to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for rate limit options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers an email rate limiter to prevent exceeding email provider limits.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEmailRateLimiter(options =>
        /// {
        ///     options.MaxRequestsPerWindow = 100;
        ///     options.WindowSize = TimeSpan.FromMinutes(1);
        ///     options.Strategy = RateLimitStrategy.FixedWindow;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailRateLimiter(
            this IServiceCollection services,
            Action<RateLimitOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new RateLimitOptions();
            configure?.Invoke(options);

            services.AddSingleton(new EmailRateLimiter(options));
            return services;
        }

        /// <summary>
        /// Adds email bulk sender service to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureRateLimiter">Optional configuration for rate limiter.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the email bulk sender service for sending multiple emails
        /// efficiently with rate limiting and progress tracking.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddEmailBulkSender(rateLimiterOptions =>
        /// {
        ///     rateLimiterOptions.MaxRequestsPerWindow = 100;
        ///     rateLimiterOptions.WindowSize = TimeSpan.FromMinutes(1);
        /// });
        /// 
        /// // Use in code
        /// var bulkSender = serviceProvider.GetRequiredService&lt;EmailBulkSender&gt;();
        /// var result = await bulkSender.SendBulkAsync(messages, options, progress => { ... });
        /// </code>
        /// </example>
        public static IServiceCollection AddEmailBulkSender(
            this IServiceCollection services,
            Action<RateLimitOptions>? configureRateLimiter = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<EmailBulkSender>(serviceProvider =>
            {
                var emailService = serviceProvider.GetRequiredService<IEmailService>();
                var logger = serviceProvider.GetService<ILogger<EmailBulkSender>>();

                EmailRateLimiter? rateLimiter = null;
                if (configureRateLimiter != null)
                {
                    var rateLimitOptions = new RateLimitOptions();
                    configureRateLimiter(rateLimitOptions);
                    rateLimiter = new EmailRateLimiter(rateLimitOptions);
                }

                return new EmailBulkSender(emailService, rateLimiter, logger);
            });

            return services;
        }
    }

    /// <summary>
    /// Template engine options for email template rendering.
    /// </summary>
    public enum TemplateEngine
    {
        /// <summary>
        /// Scriban template engine (Liquid-like syntax).
        /// </summary>
        Scriban = 0,

        /// <summary>
        /// Razor template engine (C# syntax).
        /// </summary>
        Razor = 1
    }
}

