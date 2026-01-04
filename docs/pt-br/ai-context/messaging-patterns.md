# Padrões de Mensageria para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões ao implementar mensageria assíncrona com RabbitMQ e processamento em background com Hosted Services.

---

## Integração com RabbitMQ

### Instalação de Pacotes

```xml
<PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" Version="8.*" />
<PackageReference Include="RabbitMQ.Client" Version="6.*" />
```

### Configuração

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
    typeof(ClienteCriadoConsumer).Assembly,
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

### Definição de Mensagem

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace NomeProjeto.Core.Messages
{
    public class ClienteCriadoMessage : IMessage
    {
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; }
        public string Email { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    public class ClienteAtualizadoMessage : IMessage
    {
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; }
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }

    public class ClienteExcluidoMessage : IMessage
    {
        public Guid ClienteId { get; set; }
        public DateTime ExcluidoEm { get; set; } = DateTime.UtcNow;
    }
}
```

### Producer (Publicador)

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.RabbitMQ;

namespace NomeProjeto.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IMvpRabbitMQClient _rabbitMQClient;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public ClienteService(IMvpRabbitMQClient rabbitMQClient, IUnitOfWorkAsync unitOfWork)
        {
            _rabbitMQClient = rabbitMQClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<IBusinessResult<ClienteDto>> CriarAsync(ClienteCreateDto dto)
        {
            var repository = _unitOfWork.GetRepository<Cliente>();
            var cliente = new Cliente
            {
                Nome = dto.Nome,
                Email = dto.Email
            };

            await repository.AddAsync(cliente);
            await _unitOfWork.SaveChangesAsync();

            // Publicar mensagem no RabbitMQ
            await _rabbitMQClient.PublishAsync(new ClienteCriadoMessage
            {
                ClienteId = cliente.Id,
                NomeCliente = cliente.Nome,
                Email = cliente.Email
            });

            return new BusinessResult<ClienteDto>(_mapper.Map<ClienteDto>(cliente));
        }
    }
}
```

### Consumer (Consumidor)

```csharp
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace NomeProjeto.Application.Consumers
{
    public class ClienteCriadoConsumer : IMvpRabbitMQConsumerAsync
    {
        private readonly ILogger<ClienteCriadoConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ClienteCriadoConsumer(
            ILogger<ClienteCriadoConsumer> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public string RoutingKey => nameof(ClienteCriadoMessage);
        public string QueueName => "cliente.criado.queue";

        public async Task<bool> ReceivedAsync(object message, CancellationToken cancellationToken)
        {
            try
            {
                var clienteMessage = message as ClienteCriadoMessage;
                if (clienteMessage == null)
                {
                    _logger.LogWarning("Mensagem inválida recebida");
                    return false;
                }

                _logger.LogInformation("Processando ClienteCriado: {ClienteId} - {NomeCliente}",
                    clienteMessage.ClienteId, clienteMessage.NomeCliente);

                // Processar a mensagem
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                await notificationService.EnviarEmailBoasVindasAsync(
                    clienteMessage.Email, 
                    clienteMessage.NomeCliente);

                _logger.LogInformation("ClienteCriado processado com sucesso: {ClienteId}",
                    clienteMessage.ClienteId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem ClienteCriado");
                return false;
            }
        }
    }
}
```

---

## Padrão Hosted Service

### Implementação de Background Service

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NomeProjeto.WebAPI.HostedServices
{
    public class SincronizacaoClienteHostedService : BackgroundService
    {
        private readonly ILogger<SincronizacaoClienteHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

        public SincronizacaoClienteHostedService(
            ILogger<SincronizacaoClienteHostedService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SincronizacaoClienteHostedService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessarSincronizacaoAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no SincronizacaoClienteHostedService");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("SincronizacaoClienteHostedService parado");
        }

        private async Task ProcessarSincronizacaoAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IClienteSyncService>();

            _logger.LogInformation("Iniciando sincronização de clientes...");
            var resultado = await syncService.SincronizarClientesAsync(cancellationToken);
            _logger.LogInformation("Sincronização de clientes concluída: {Count} registros processados", resultado);
        }
    }
}

// Registro em Startup.cs ou Program.cs
services.AddHostedService<SincronizacaoClienteHostedService>();
```

### Consumer RabbitMQ como Hosted Service

```csharp
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Infrastructure.RabbitMQ;

namespace NomeProjeto.WebAPI.HostedServices
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
            _logger.LogInformation("Iniciando consumidores RabbitMQ...");

            try
            {
                await _rabbitMQClient.StartConsumersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar consumidores RabbitMQ");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando consumidores RabbitMQ...");
            await _rabbitMQClient.StopConsumersAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
```

---

## Pipeline com Mensageria

### Pipeline de Processamento de Mensagem

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;

namespace NomeProjeto.Application.Pipelines
{
    public class ClienteCriadoPipeline
    {
        private readonly IPipelineAsync _pipeline;

        public ClienteCriadoPipeline(IPipelineAsync pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<IPipelineMessage> ProcessarAsync(ClienteCriadoMessage message)
        {
            var pipelineMessage = new PipelineMessage();
            pipelineMessage.AddContent("Message", message);

            return await _pipeline
                .AddAsync<ValidarClienteOperation>()
                .AddAsync<EnriquecerDadosClienteOperation>()
                .AddAsync<EnviarEmailBoasVindasOperation>()
                .AddAsync<AtualizarCRMOperation>()
                .ExecuteAsync(pipelineMessage);
        }
    }

    public class ValidarClienteOperation : IOperationAsync
    {
        public async Task ExecuteAsync(IPipelineMessage input)
        {
            var message = input.GetContent<ClienteCriadoMessage>("Message");
            
            if (string.IsNullOrEmpty(message.Email))
            {
                input.SetLock();
                input.Messages.Add(new MessageResult("Email é obrigatório", MessageType.Error));
            }

            await Task.CompletedTask;
        }
    }

    public class EnviarEmailBoasVindasOperation : IOperationAsync
    {
        private readonly IEmailService _emailService;

        public EnviarEmailBoasVindasOperation(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task ExecuteAsync(IPipelineMessage input)
        {
            if (input.IsLocked) return;

            var message = input.GetContent<ClienteCriadoMessage>("Message");
            await _emailService.EnviarEmailBoasVindasAsync(message.Email, message.NomeCliente);
        }
    }
}
```

---

## Tratamento de Erros e Retry

### Configuração de Retry

```csharp
// appsettings.json
{
  "RabbitMQ": {
    "MaxRetryCount": 3,
    "RetryIntervalMs": 5000,
    "DeadLetterExchange": "mvp24hours.dlx"
  }
}

// Consumer com retry manual
public class ClienteCriadoConsumer : IMvpRabbitMQConsumerAsync
{
    private readonly ILogger<ClienteCriadoConsumer> _logger;
    private readonly int _maxRetries = 3;

    public async Task<bool> ReceivedAsync(object message, CancellationToken cancellationToken)
    {
        var clienteMessage = message as ClienteCriadoMessage;
        var tentativa = 0;

        while (tentativa < _maxRetries)
        {
            try
            {
                await ProcessarMensagemAsync(clienteMessage);
                return true;
            }
            catch (Exception ex) when (tentativa < _maxRetries - 1)
            {
                tentativa++;
                _logger.LogWarning(ex, "Tentativa {Tentativa}/{MaxRetries} para mensagem {MessageId}",
                    tentativa, _maxRetries, clienteMessage.ClienteId);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falhou após {MaxRetries} tentativas para mensagem {MessageId}",
                    _maxRetries, clienteMessage.ClienteId);
                return false;
            }
        }

        return false;
    }
}
```

---

## Exemplo Completo: API de Clientes com RabbitMQ

### Estrutura do Projeto

```
Solution/
├── ClienteAPI.Core/
│   ├── Entities/
│   │   └── Cliente.cs
│   ├── Messages/
│   │   ├── ClienteCriadoMessage.cs
│   │   └── ClienteAtualizadoMessage.cs
│   └── Contract/
│       └── IClienteService.cs
├── ClienteAPI.Infrastructure/
│   └── Data/
│       └── DataContext.cs
├── ClienteAPI.Application/
│   ├── Services/
│   │   └── ClienteService.cs
│   └── Consumers/
│       └── ClienteCriadoConsumer.cs
└── ClienteAPI.WebAPI/
    ├── Controllers/
    │   └── ClienteController.cs
    ├── HostedServices/
    │   └── RabbitMQConsumerHostedService.cs
    └── Extensions/
        └── ServiceBuilderExtensions.cs
```

### Registro de Serviços

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
{
    // Banco de Dados
    services.AddDbContext<DataContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    services.AddMvp24HoursDbContext<DataContext>();
    services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

    // RabbitMQ
    services.AddMvp24HoursRabbitMQ(
        typeof(ClienteCriadoConsumer).Assembly,
        configuration.GetSection("RabbitMQ"));

    // Hosted Services
    services.AddHostedService<RabbitMQConsumerHostedService>();

    // Services
    services.AddScoped<IClienteService, ClienteService>();

    // Health Checks
    services.AddHealthChecks()
        .AddSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .AddRabbitMQ(configuration["RabbitMQ:ConnectionString"]);

    return services;
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Observabilidade](observability-patterns.md)

