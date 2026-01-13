# Messaging Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns when implementing asynchronous messaging with RabbitMQ and background processing with Hosted Services.

---

## RabbitMQ Integration

### Package Installation

```xml
<PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" Version="9.*" />
<PackageReference Include="RabbitMQ.Client" Version="6.*" />
```

### Configuration

```csharp
// appsettings.json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "DispatchConsumersAsync": true,
    "Exchange": "mvp24hours.exchange",
    "MaxRetryCount": 3
  }
}

// ServiceBuilderExtensions.cs
services.AddMvp24HoursRabbitMQ(
    typeof(CustomerCreatedConsumer).Assembly,
    configuration.GetSection("RabbitMQ"),
    options =>
    {
        options.Exchange = configuration["RabbitMQ:Exchange"];
        options.MaxRetryCount = int.Parse(configuration["RabbitMQ:MaxRetryCount"]);
    });

// Health Check
services.AddHealthChecks()
    .AddRabbitMQ(
        $"amqp://{configuration["RabbitMQ:UserName"]}:{configuration["RabbitMQ:Password"]}@{configuration["RabbitMQ:HostName"]}:{configuration["RabbitMQ:Port"]}/{configuration["RabbitMQ:VirtualHost"]}",
        name: "RabbitMQ",
        failureStatus: HealthStatus.Degraded);
```

### Message Definition

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace ProjectName.Core.Messages
{
    public class CustomerCreatedMessage : IMessage
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CustomerUpdatedMessage : IMessage
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CustomerDeletedMessage : IMessage
    {
        public Guid CustomerId { get; set; }
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### Producer (Publisher)

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.RabbitMQ;

namespace ProjectName.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IMvpRabbitMQClient _rabbitMQClient;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public CustomerService(IMvpRabbitMQClient rabbitMQClient, IUnitOfWorkAsync unitOfWork)
        {
            _rabbitMQClient = rabbitMQClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
        {
            var repository = _unitOfWork.GetRepository<Customer>();
            var customer = new Customer
            {
                Name = dto.Name,
                Email = dto.Email
            };

            await repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            // Publish message to RabbitMQ
            await _rabbitMQClient.PublishAsync(new CustomerCreatedMessage
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Email = customer.Email
            });

            return new BusinessResult<CustomerDto>(_mapper.Map<CustomerDto>(customer));
        }
    }
}
```

### Consumer (Subscriber)

```csharp
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace ProjectName.Application.Consumers
{
    public class CustomerCreatedConsumer : IMvpRabbitMQConsumerAsync
    {
        private readonly ILogger<CustomerCreatedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CustomerCreatedConsumer(
            ILogger<CustomerCreatedConsumer> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public string RoutingKey => nameof(CustomerCreatedMessage);
        public string QueueName => "customer.created.queue";

        public async Task<bool> ReceivedAsync(object message, CancellationToken cancellationToken)
        {
            try
            {
                var customerMessage = message as CustomerCreatedMessage;
                if (customerMessage == null)
                {
                    _logger.LogWarning("Invalid message received");
                    return false;
                }

                _logger.LogInformation("Processing CustomerCreated: {CustomerId} - {CustomerName}",
                    customerMessage.CustomerId, customerMessage.CustomerName);

                // Process the message
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                await notificationService.SendWelcomeEmailAsync(
                    customerMessage.Email, 
                    customerMessage.CustomerName);

                _logger.LogInformation("CustomerCreated processed successfully: {CustomerId}",
                    customerMessage.CustomerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CustomerCreated message");
                return false;
            }
        }
    }
}
```

### Multiple Consumers Registration

```csharp
// ServiceBuilderExtensions.cs
services.AddMvp24HoursRabbitMQ(
    typeof(CustomerCreatedConsumer).Assembly,
    configuration.GetSection("RabbitMQ"),
    options =>
    {
        options.Exchange = "mvp24hours.exchange";
        options.MaxRetryCount = 3;
    });

// All consumers in the assembly implementing IMvpRabbitMQConsumerAsync 
// will be automatically registered
```

---

## Hosted Service Pattern

### Background Service Implementation

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectName.WebAPI.HostedServices
{
    public class CustomerSyncHostedService : BackgroundService
    {
        private readonly ILogger<CustomerSyncHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public CustomerSyncHostedService(
            ILogger<CustomerSyncHostedService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CustomerSyncHostedService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSyncAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CustomerSyncHostedService");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("CustomerSyncHostedService stopped");
        }

        private async Task ProcessSyncAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ICustomerSyncService>();

            _logger.LogInformation("Starting customer sync...");
            var result = await syncService.SyncCustomersAsync(cancellationToken);
            _logger.LogInformation("Customer sync completed: {Count} records processed", result);
        }
    }
}

// Registration in Startup.cs or Program.cs
services.AddHostedService<CustomerSyncHostedService>();
```

### RabbitMQ Consumer as Hosted Service

```csharp
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Infrastructure.RabbitMQ;

namespace ProjectName.WebAPI.HostedServices
{
    public class RabbitMQConsumerHostedService : BackgroundService
    {
        private readonly IMvpRabbitMQClient _rabbitMQClient;
        private readonly ILogger<RabbitMQConsumerHostedService> _logger;

        public RabbitMQConsumerHostedService(
            IMvpRabbitMQClient rabbitMQClient,
            ILogger<RabbitMQConsumerHostedService> logger)
        {
            _rabbitMQClient = rabbitMQClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting RabbitMQ consumers...");

            try
            {
                await _rabbitMQClient.StartConsumersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RabbitMQ consumers");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ consumers...");
            await _rabbitMQClient.StopConsumersAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
```

---

## Pipeline with Messaging

### Message Processing Pipeline

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;

namespace ProjectName.Application.Pipelines
{
    public class CustomerCreatedPipeline
    {
        private readonly IPipelineAsync _pipeline;

        public CustomerCreatedPipeline(IPipelineAsync pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<IPipelineMessage> ProcessAsync(CustomerCreatedMessage message)
        {
            var pipelineMessage = new PipelineMessage();
            pipelineMessage.AddContent("Message", message);

            return await _pipeline
                .AddAsync<ValidateCustomerOperation>()
                .AddAsync<EnrichCustomerDataOperation>()
                .AddAsync<SendWelcomeEmailOperation>()
                .AddAsync<UpdateCRMOperation>()
                .ExecuteAsync(pipelineMessage);
        }
    }

    public class ValidateCustomerOperation : IOperationAsync
    {
        public async Task ExecuteAsync(IPipelineMessage input)
        {
            var message = input.GetContent<CustomerCreatedMessage>("Message");
            
            if (string.IsNullOrEmpty(message.Email))
            {
                input.SetLock();
                input.Messages.Add(new MessageResult("Email is required", MessageType.Error));
            }

            await Task.CompletedTask;
        }
    }

    public class SendWelcomeEmailOperation : IOperationAsync
    {
        private readonly IEmailService _emailService;

        public SendWelcomeEmailOperation(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task ExecuteAsync(IPipelineMessage input)
        {
            if (input.IsLocked) return;

            var message = input.GetContent<CustomerCreatedMessage>("Message");
            await _emailService.SendWelcomeEmailAsync(message.Email, message.CustomerName);
        }
    }
}
```

---

## Error Handling and Retry

### Retry Configuration

```csharp
// appsettings.json
{
  "RabbitMQ": {
    "MaxRetryCount": 3,
    "RetryIntervalMs": 5000,
    "DeadLetterExchange": "mvp24hours.dlx"
  }
}

// Consumer with manual retry
public class CustomerCreatedConsumer : IMvpRabbitMQConsumerAsync
{
    private readonly ILogger<CustomerCreatedConsumer> _logger;
    private readonly int _maxRetries = 3;

    public async Task<bool> ReceivedAsync(object message, CancellationToken cancellationToken)
    {
        var customerMessage = message as CustomerCreatedMessage;
        var retryCount = 0;

        while (retryCount < _maxRetries)
        {
            try
            {
                await ProcessMessageAsync(customerMessage);
                return true;
            }
            catch (Exception ex) when (retryCount < _maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} for message {MessageId}",
                    retryCount, _maxRetries, customerMessage.CustomerId);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed after {MaxRetries} retries for message {MessageId}",
                    _maxRetries, customerMessage.CustomerId);
                return false;
            }
        }

        return false;
    }
}
```

### Dead Letter Queue Pattern

```csharp
public class DeadLetterConsumer : IMvpRabbitMQConsumerAsync
{
    private readonly ILogger<DeadLetterConsumer> _logger;
    private readonly IDeadLetterRepository _deadLetterRepository;

    public string RoutingKey => "deadletter.#";
    public string QueueName => "deadletter.queue";

    public async Task<bool> ReceivedAsync(object message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Received dead letter message: {Message}", message);

            await _deadLetterRepository.SaveAsync(new DeadLetterRecord
            {
                Message = JsonSerializer.Serialize(message),
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dead letter message");
            return false;
        }
    }
}
```

---

## Complete Example: Customer API with RabbitMQ

### Project Structure

```
Solution/
├── CustomerAPI.Core/
│   ├── Entities/
│   │   └── Customer.cs
│   ├── Messages/
│   │   ├── CustomerCreatedMessage.cs
│   │   └── CustomerUpdatedMessage.cs
│   └── Contract/
│       └── ICustomerService.cs
├── CustomerAPI.Infrastructure/
│   └── Data/
│       └── DataContext.cs
├── CustomerAPI.Application/
│   ├── Services/
│   │   └── CustomerService.cs
│   └── Consumers/
│       └── CustomerCreatedConsumer.cs
└── CustomerAPI.WebAPI/
    ├── Controllers/
    │   └── CustomerController.cs
    ├── HostedServices/
    │   └── RabbitMQConsumerHostedService.cs
    └── Extensions/
        └── ServiceBuilderExtensions.cs
```

### Service Registration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
{
    // Database
    services.AddDbContext<DataContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    services.AddMvp24HoursDbContext<DataContext>();
    services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

    // RabbitMQ
    services.AddMvp24HoursRabbitMQ(
        typeof(CustomerCreatedConsumer).Assembly,
        configuration.GetSection("RabbitMQ"));

    // Hosted Services
    services.AddHostedService<RabbitMQConsumerHostedService>();

    // Services
    services.AddScoped<ICustomerService, CustomerService>();

    // Health Checks
    services.AddHealthChecks()
        .AddSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .AddRabbitMQ(configuration["RabbitMQ:ConnectionString"]);

    return services;
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Observability Patterns](observability-patterns.md)

