# System.Threading.Channels (Produtor/Consumidor)

## Visão Geral

**System.Threading.Channels** é uma biblioteca nativa do .NET para implementar padrões de produtor/consumidor de alta performance. Ela fornece estruturas de dados thread-safe e async-friendly que permitem comunicação eficiente entre produtores e consumidores.

O Mvp24Hours integra com System.Threading.Channels para fornecer:

- **Filas in-memory de alta performance** com suporte a backpressure
- **Comunicação entre operações de Pipeline** para processamento streaming de dados
- **Processamento em batch do RabbitMQ** com buffer eficiente de mensagens
- **Padrões genéricos de produtor/consumidor** para qualquer workload async

## Por Que Usar Channels?

Channels substituem abordagens tradicionais como `ConcurrentQueue`, `BlockingCollection` e locks manuais com uma API moderna e async-first:

| Abordagem Tradicional | Abordagem com Channels |
|----------------------|------------------------|
| `ConcurrentQueue + AutoResetEvent` | `Channel<T>` |
| Backpressure manual com semáforos | `BoundedChannel` com backpressure automático |
| Chamadas bloqueantes `Take()` | `ReadAsync()` assíncrono |
| Tratamento complexo de cancelamento | Suporte nativo a `CancellationToken` |

## Conceitos Principais

### Tipos de Channel

```csharp
// Unbounded: Sem limite, sem backpressure
using var unbounded = Channels.CreateUnbounded<Order>();

// Bounded: Capacidade limitada, aplica backpressure
using var bounded = Channels.CreateBounded<Order>(100);

// High-throughput: Otimizado para performance
using var fast = Channels.CreateHighThroughput<Order>(1000);

// Estratégias de drop: Lidar com overflow graciosamente
using var dropOldest = Channels.CreateDropOldest<Order>(100);
using var dropNewest = Channels.CreateDropNewest<Order>(100);
```

### Modos de Canal Cheio (Bounded)

| Modo | Comportamento |
|------|--------------|
| `Wait` | Bloqueia produtor até espaço disponível (padrão) |
| `DropOldest` | Remove item mais antigo para abrir espaço |
| `DropNewest` | Descarta o novo item sendo escrito |
| `DropWrite` | Retorna false para TryWrite, lança exceção para WriteAsync |

## Uso Básico

### Criando um Channel

```csharp
using Mvp24Hours.Core.Infrastructure.Channels;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;

// Usando factory (amigável para DI)
var factory = new ChannelFactory();
using var channel = factory.CreateBounded<Order>(100);

// Usando helper estático
using var channel = Channels.CreateBounded<Order>(100);

// Usando construtor com opções
using var channel = new MvpChannel<Order>(new ChannelOptions
{
    IsBounded = true,
    Capacity = 100,
    FullMode = BoundedChannelFullMode.Wait
});
```

### Escrevendo em um Channel

```csharp
// Item único
await channel.Writer.WriteAsync(new Order { Id = 1 });

// Múltiplos itens
await channel.Writer.WriteManyAsync(orders);

// Escrita não-bloqueante
if (channel.Writer.TryWrite(order))
{
    Console.WriteLine("Pedido enfileirado");
}
else
{
    Console.WriteLine("Canal cheio!");
}

// Sinalizar conclusão
channel.Writer.TryComplete();
```

### Lendo de um Channel

```csharp
// Item único
var order = await channel.Reader.ReadAsync();

// Todos os itens (streaming)
await foreach (var order in channel.Reader.ReadAllAsync())
{
    await ProcessOrderAsync(order);
}

// Leitura em batch
await foreach (var batch in channel.Reader.ReadBatchAsync(
    batchSize: 10, 
    timeout: TimeSpan.FromSeconds(5)))
{
    await ProcessBatchAsync(batch);
}

// Leitura não-bloqueante
if (channel.Reader.TryRead(out var order))
{
    await ProcessOrderAsync(order);
}
```

## Padrão Produtor/Consumidor

### Produtor/Consumidor Simples

```csharp
using Mvp24Hours.Core.Infrastructure.Channels;

// Criar produtor-consumidor com 4 workers
await using var pc = new ProducerConsumer<Order>(
    processor: async (order, ct) => 
    {
        await SaveOrderAsync(order, ct);
        await SendConfirmationAsync(order, ct);
    },
    workerCount: 4,
    options: new ProducerConsumerOptions 
    { 
        Capacity = 100,
        ContinueOnError = true 
    });

// Iniciar workers
pc.Start();

// Produzir itens
foreach (var order in orders)
{
    await pc.ProduceAsync(order);
}

// Sinalizar conclusão e aguardar
pc.Complete();
await pc.WaitForCompletionAsync();
```

### Produtor/Consumidor com Resultados

```csharp
await using var pc = new ProducerConsumer<Order, ProcessedOrder>(
    processor: async (order, ct) => 
    {
        var result = await ProcessOrderAsync(order, ct);
        return new ProcessedOrder { OrderId = order.Id, Result = result };
    },
    workerCount: Environment.ProcessorCount);

pc.Start();

// Produzir em background
_ = Task.Run(async () =>
{
    foreach (var order in orders)
    {
        await pc.ProduceAsync(order);
    }
    pc.Complete();
});

// Consumir resultados
await foreach (var result in pc.GetResultsAsync())
{
    Console.WriteLine($"Pedido {result.OrderId}: {result.Result}");
}
```

## Integração com Pipeline

### Channel Pipeline

```csharp
using Mvp24Hours.Infrastructure.Pipe.Channels;

// Criar pipeline multi-estágio
var pipeline = new ChannelPipeline<RawOrder, ProcessedOrder>(
    options: new ChannelPipelineOptions
    {
        ChannelCapacity = 100,
        MaxDegreeOfParallelism = 4
    },
    logger: logger);

// Adicionar estágios de processamento
pipeline
    .AddStage<RawOrder, ValidatedOrder>(order => ValidateOrder(order))
    .AddStageAsync<ValidatedOrder, EnrichedOrder>(
        async (order, ct) => await EnrichOrderAsync(order, ct))
    .AddStage<EnrichedOrder, ProcessedOrder>(order => FinalizeOrder(order));

// Processar itens com saída streaming
await foreach (var result in pipeline.ProcessAsync(rawOrders))
{
    await SaveResultAsync(result);
}

// Ou processar em paralelo
await foreach (var result in pipeline.ProcessParallelAsync(
    rawOrders, 
    maxDegreeOfParallelism: 4))
{
    await SaveResultAsync(result);
}
```

## Integração com RabbitMQ

### Processamento em Batch Baseado em Channels

```csharp
using Mvp24Hours.Infrastructure.RabbitMQ.Channels;

// Criar processador de batch com channels
await using var processor = new ChannelBatchProcessor<Order>(
    options: new BatchConsumerOptions
    {
        MaxBatchSize = 100,
        MinBatchSize = 10,
        BatchTimeout = TimeSpan.FromSeconds(5)
    },
    serviceProvider: serviceProvider,
    messageSerializer: serializer,
    logger: logger,
    channel: rabbitChannel);

// Iniciar processamento em background
processor.Start();

// Mensagens são adicionadas via consumer do RabbitMQ
consumer.Received += async (s, e) =>
{
    await processor.AddMessageAsync(e);
};

// Shutdown gracioso
await processor.FlushAsync();
```

## Injeção de Dependência

### Registro

```csharp
using Mvp24Hours.Core.Extensions;

// Registro básico
services.AddMvpChannels();

// Registrar channels específicos
services.AddBoundedChannel<Order>(100);
services.AddUnboundedChannel<Event>();
services.AddHighThroughputChannel<LogEntry>(1000);
services.AddDropOldestChannel<Metric>(500);

// Registrar com opções customizadas
services.AddChannel<Message>(options =>
{
    options.Capacity = 200;
    options.FullMode = BoundedChannelFullMode.DropNewest;
    options.SingleReader = true;
});

// Channels com chave para diferentes propósitos
services.AddKeyedBoundedChannel<Order>("priority", 50);
services.AddKeyedBoundedChannel<Order>("standard", 200);
```

### Uso em Serviços

```csharp
public class OrderProcessor
{
    private readonly IChannel<Order> _orderChannel;
    private readonly IChannelWriter<Order> _writer;
    private readonly IChannelReader<Order> _reader;

    public OrderProcessor(IChannel<Order> channel)
    {
        _orderChannel = channel;
        _writer = channel.Writer;
        _reader = channel.Reader;
    }

    public async Task QueueOrderAsync(Order order)
    {
        await _writer.WriteAsync(order);
    }

    public async Task ProcessOrdersAsync(CancellationToken ct)
    {
        await foreach (var order in _reader.ReadAllAsync(ct))
        {
            await ProcessAsync(order);
        }
    }
}
```

## Boas Práticas

### 1. Escolha o Tipo Certo de Channel

```csharp
// Para produtor-consumidor com controle de fluxo
var channel = Channels.CreateBounded<T>(capacity);

// Para streaming de eventos onde o mais recente importa
var channel = Channels.CreateDropOldest<T>(capacity);

// Para fire-and-forget com buffer ilimitado
var channel = Channels.CreateUnbounded<T>();
```

### 2. Trate o Backpressure

```csharp
// Opção 1: Aguardar (padrão)
await channel.Writer.WriteAsync(item); // Bloqueia quando cheio

// Opção 2: Tentar não-bloqueante
if (!channel.Writer.TryWrite(item))
{
    // Tratar overflow
    await _fallbackQueue.EnqueueAsync(item);
}

// Opção 3: Usar estratégias de drop
var channel = Channels.CreateDropOldest<T>(capacity);
```

### 3. Sempre Complete os Writers

```csharp
try
{
    foreach (var item in items)
    {
        await channel.Writer.WriteAsync(item);
    }
}
finally
{
    channel.Writer.TryComplete();
}
```

### 4. Use CancellationTokens

```csharp
await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
{
    await ProcessAsync(item, cancellationToken);
}
```

## Considerações de Performance

| Cenário | Recomendação |
|---------|--------------|
| Alta vazão | Use `SingleReader`/`SingleWriter` quando aplicável |
| Sensível a memória | Use channels bounded com capacidade apropriada |
| Dados em tempo real | Use `DropOldest` para priorizar dados recentes |
| Processamento em batch | Use `ReadBatchAsync` para reduzir overhead |

## Migração de ConcurrentQueue

### Antes (ConcurrentQueue)

```csharp
private readonly ConcurrentQueue<Order> _queue = new();
private readonly AutoResetEvent _signal = new(false);

public void Enqueue(Order order)
{
    _queue.Enqueue(order);
    _signal.Set();
}

public async Task ProcessAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        _signal.WaitOne(TimeSpan.FromSeconds(1));
        while (_queue.TryDequeue(out var order))
        {
            await ProcessOrderAsync(order);
        }
    }
}
```

### Depois (Channel)

```csharp
private readonly IChannel<Order> _channel;

public async Task EnqueueAsync(Order order)
{
    await _channel.Writer.WriteAsync(order);
}

public async Task ProcessAsync(CancellationToken ct)
{
    await foreach (var order in _channel.Reader.ReadAllAsync(ct))
    {
        await ProcessOrderAsync(order);
    }
}
```

## Veja Também

- [Rate Limiting](rate-limiting.md) - Rate limiting com `System.Threading.RateLimiting`
- [Periodic Timer](periodic-timer.md) - Padrões modernos de timer
- [Time Provider](time-provider.md) - Abstração de tempo para testes

