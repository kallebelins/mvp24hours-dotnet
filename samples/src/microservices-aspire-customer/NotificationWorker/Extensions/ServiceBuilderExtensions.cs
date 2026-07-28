using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using NotificationWorker.Consumers;
using NotificationWorker.Data;
using NotificationWorker.Services;

namespace NotificationWorker.Extensions;

/// <summary>
/// Extension methods for registering NotificationWorker services.
/// </summary>
public static class ServiceBuilderExtensions
{
    /// <summary>
    /// Registers the in-memory EF Core context for notification logs.
    /// </summary>
    public static IServiceCollection AddNotificationDbContext(this IServiceCollection services)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseInMemoryDatabase("MyAspireNotificationDb"));

        return services;
    }

    /// <summary>
    /// Registers application services.
    /// </summary>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }

    /// <summary>
    /// Configures the RabbitMQ consumer.
    /// <para>
    /// Aspire injects the RabbitMQ connection string via
    /// <c>ConnectionStrings__messaging</c> when running with AppHost.
    /// Falls back to localhost for standalone development.
    /// </para>
    /// </summary>
    public static IServiceCollection AddNotificationMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitConnectionString = configuration.GetConnectionString("messaging")
            ?? "amqp://guest:guest@localhost:5672";

        services.AddMvpRabbitMQ(rabbit =>
        {
            rabbit.Host(rabbitConnectionString, connection =>
            {
                connection.DispatchConsumersAsync(true);
                connection.RetryCount(3);
            });

            rabbit.AddConsumersFromAssemblyContaining<CustomerCreatedConsumer>();

            rabbit.ConfigureClient(clientOptions =>
            {
                clientOptions.Exchange = "amq.direct";
                clientOptions.MaxRedeliveredCount = 1;
                clientOptions.QueueArguments = new Dictionary<string, object>
                {
                    { "x-queue-mode", "lazy" },
                    { "x-dead-letter-exchange", "dead-letter-amq.direct" }
                };
                clientOptions.DeadLetter = new RabbitMQOptions
                {
                    Exchange = "dead-letter-amq.direct",
                    QueueArguments = new Dictionary<string, object>
                    {
                        { "x-queue-mode", "lazy" }
                    }
                };
            });
        });

        return services;
    }

    /// <summary>
    /// Starts the RabbitMQ background polling hosted service.
    /// </summary>
    public static IServiceCollection AddNotificationHostedService(this IServiceCollection services)
    {
        services.AddOptions<RabbitMQHostedOptions>()
            .Configure<Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IMvpRabbitMQClient>((options, client) =>
            {
                options.Callback = _ => client.Consume();
                options.DueTime = TimeSpan.Zero;
                options.Period = TimeSpan.FromSeconds(3);
            });

        services.AddMvp24HoursHostedService();
        return services;
    }

    /// <summary>
    /// Registers health checks for RabbitMQ.
    /// </summary>
    public static IServiceCollection AddNotificationHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        var rabbitCs = configuration.GetConnectionString("messaging");
        if (!string.IsNullOrWhiteSpace(rabbitCs))
        {
            builder.AddRabbitMQ(
                rabbitCs,
                name: "RabbitMQ",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["ready"]);
        }

        return services;
    }
}
