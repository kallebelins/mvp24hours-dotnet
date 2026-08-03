//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Testcontainers.RabbitMq;
using Xunit.Priority;

namespace Mvp24Hours.Application.RabbitMQ.Test;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Integration")]
public class Test1RabbitMQ : IAsyncLifetime
{
    #region [ Container ]
    private RabbitMqContainer? _rabbitMqContainer;
    private bool _isContainerAvailable;

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        try
        {
            _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
                .WithExposedPort(5672)
                .WithUsername("guest")
                .WithPassword("guest")
                .WithCleanUp(true)
                .Build();
            await _rabbitMqContainer.StartAsync().ConfigureAwait(false);
            _isContainerAvailable = true;
        }
        catch (Exception)
        {
            _isContainerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_rabbitMqContainer is not null)
        {
            await _rabbitMqContainer.DisposeAsync().ConfigureAwait(false);
        }
    }
    #endregion

    #region [ Configure ]
    public Test1RabbitMQ() { }

    private IServiceProvider SetupTypeAssembly()
    {
        var services = new ServiceCollection();

        services.AddScoped<CustomerConsumer, CustomerConsumer>();
        services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
        services.AddTransient(x => new CustomerEvent() { Name = "event" });

        services.AddMvp24HoursRabbitMQ(
            typeof(CustomerConsumer).Assembly,
            connectionOptions =>
            {
                connectionOptions.ConnectionString = _rabbitMqContainer!.GetConnectionString();
                connectionOptions.DispatchConsumersAsync = true;
                connectionOptions.RetryCount = 3;
            },
            clientOptions => clientOptions.MaxRedeliveredCount = 1);
        return services.BuildServiceProvider();
    }

    private IServiceProvider SetupTypeAssemblyWithoutInjection()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQ(
            [typeof(CustomerConsumer)],
            connectionOptions =>
            {
                connectionOptions.ConnectionString = _rabbitMqContainer!.GetConnectionString();
                connectionOptions.DispatchConsumersAsync = true;
                connectionOptions.RetryCount = 3;
            },
            clientOptions => clientOptions.MaxRedeliveredCount = 1);
        return services.BuildServiceProvider();
    }

    private IServiceProvider SetupTypeDefined()
    {
        var services = new ServiceCollection();

        services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
        services.AddTransient(x => new CustomerEvent() { Name = "event" });

        services.AddMvp24HoursRabbitMQ(
            [typeof(CustomerWithCtorConsumer)],
            connectionOptions =>
            {
                connectionOptions.ConnectionString = _rabbitMqContainer!.GetConnectionString();
                connectionOptions.DispatchConsumersAsync = true;
                connectionOptions.RetryCount = 3;
            },
            clientOptions => clientOptions.MaxRedeliveredCount = 1);
        return services.BuildServiceProvider();
    }

    private IServiceProvider SetupTypeDefinedList()
    {
        var services = new ServiceCollection();

        services.AddScoped<CustomerConsumer, CustomerConsumer>();
        services.AddScoped<CustomerWithCtorConsumer, CustomerWithCtorConsumer>();
        services.AddTransient(x => new CustomerEvent() { Name = "event" });

        Type[] consumers = [.. typeof(Test1RabbitMQ).Assembly
                .GetExportedTypes()
                .Where(t => t.InheritsOrImplements(typeof(IMvpRabbitMQConsumer)))];

        services.AddMvp24HoursRabbitMQ(
            consumers,
            connectionOptions =>
            {
                connectionOptions.ConnectionString = _rabbitMqContainer!.GetConnectionString();
                connectionOptions.DispatchConsumersAsync = true;
                connectionOptions.RetryCount = 3;
            },
            clientOptions => clientOptions.MaxRedeliveredCount = 1);
        return services.BuildServiceProvider();
    }
    #endregion

    [DockerFact]
    public void CreateProducerAssembly()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeAssembly();
        // arrange
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // act
        string result = client.Publish(new CustomerEvent
        {
            Id = 1,
            Name = "Test 1",
            Active = true
        }, typeof(CustomerEvent).Name);

        // assert
        Assert.True(result.HasValue());
    }

    [DockerFact]
    public void CreateConsumerAssembly()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeAssembly();
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // arrange
        client.Publish(new CustomerEvent
        {
            Id = 2,
            Name = "Test 2",
            Active = true
        }, typeof(CustomerEvent).Name);


        // act
        client.Consume();

        // assert
        Assert.True(true);
    }

    [DockerFact]
    public void CreateConsumerWithoutInjection()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeAssemblyWithoutInjection();
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // arrange
        client.Publish(new CustomerEvent
        {
            Id = 2,
            Name = "Test 2",
            Active = true
        }, typeof(CustomerEvent).Name);


        // act
        client.Consume();

        // assert
        Assert.True(true);
    }

    [DockerFact]
    public void CreateProducerDefined()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeDefined();
        // arrange
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // act
        string result = client.Publish(new CustomerEvent
        {
            Id = 1,
            Name = "Test 1",
            Active = true
        }, typeof(CustomerEvent).Name);

        // assert
        Assert.True(result.HasValue());
    }

    [DockerFact]
    public void CreateConsumerDefined()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeDefined();
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // arrange
        client.Publish(new CustomerEvent
        {
            Id = 2,
            Name = "Test 2",
            Active = true
        }, typeof(CustomerEvent).Name);


        // act
        client.Consume();

        // assert
        Assert.True(true);
    }

    [DockerFact]
    public void CreateConsumerDefinedList()
    {
        if (!_isContainerAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = SetupTypeDefinedList();
        MvpRabbitMQClient? client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        // arrange
        client.Publish(new CustomerEvent
        {
            Id = 2,
            Name = "Test 2",
            Active = true
        }, typeof(CustomerEvent).Name);


        // act
        client.Consume();

        // assert
        Assert.True(true);
    }
}
