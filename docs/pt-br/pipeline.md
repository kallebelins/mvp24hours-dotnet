# Pipeline (Pipe and Filters Pattern)
É um padrão de projeto que representa um tubo com diversas operações (filtros), executadas de forma sequencial, com o intuito de trafegar, integrar e/ou manipular um pacote/mensagem.

## Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.Pipe -Version 9.1.x
```

> 📚 Para recursos avançados como middleware pattern, pipelines tipados, fork/join, checkpoint/resume, resiliência e observabilidade, consulte a documentação avançada abaixo.

## Configuração Básica
```csharp
/// Program.cs
builder.Services.AddMvp24HoursPipeline(options => // async => AddMvp24HoursPipelineAsync
{
    options.IsBreakOnFail = false;
});
```

## Configuração com Factory
```csharp
/// Program.cs
builder.Services.AddMvp24HoursPipeline(factory: (_) => // async => AddMvp24HoursPipelineAsync
{
    var pipeline = new Pipeline(); // async => PipelineAsync
    pipeline.AddInterceptors(input =>
    {
        input.AddContent<int>("factory", 1);
        System.Diagnostics.Trace.WriteLine("Interceptor factory.");
    }, Core.Enums.Infrastructure.PipelineInterceptorType.PostOperation);
    return pipeline;
});
```

## Operações/Filtros

### Adicionando Anônimas
```csharp
// adicionar operação/filtro como action
pipeline.Add(_ =>
{
    Trace.WriteLine("Test 1");
});
```

### Adicionando Instâncias
Para criar uma operação basta implementar uma IOperation ou uma OperationBase:

#### Operações/Filtros Síncronas
```csharp
/// MyOperation.cs
public class MyOperation : OperationBase
{
    public override bool IsRequired => false; // indica se a operação irá executar mesmo com o pacote bloqueado

    public override void Execute(IPipelineMessage input)
    {
        // executa ação
        return input;
    }
}

// adicionar ao pipeline
pipeline.Add<MyOperation>();
```

#### Rollback Síncronos
```csharp
/// MyOperation.cs
public class MyOperation : OperationBase
{
    public override void Execute(IPipelineMessage input) 
	{ 
		// executa ação 
	}
	
	public override void Rollback(IPipelineMessage input)
	{
		// desfaz a ação executada
	}
}

// Habilita o pipeline a executar o rollback em caso de erro. Default é false.
pipeline.ForceRollbackOnFalure = true;

// adicionar ao pipeline
pipeline.Add<MyOperation>();
```

#### Operações/Filtros Assíncronas
```csharp
/// MyOperationAsync.cs
public class MyOperationAsync : OperationBaseAsync
{
    public override bool IsRequired => false; // indica se a operação irá executar mesmo com o pacote bloqueado

    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        await Task.CompletedTask;
    }
}

// adicionar ao pipeline assíncrono
pipeline.Add<MyOperationAsync>();
```

#### Rollback Assíncronos
```csharp
/// MyOperationAsync.cs
public class MyOperationAsync : OperationBaseAsync
{
    public override async Task ExecuteAsync(IPipelineMessage input)
    {
		// executa ação
        await Task.CompletedTask;
    }
	
	public override async Task RollbackAsync(IPipelineMessage input)
	{
		// desfaz a ação executada
		await Task.CompletedTask;
	}
}

// Habilita o pipeline a executar o rollback em caso de erro. Default é false.
pipeline.ForceRollbackOnFalure = true;

// adicionar ao pipeline assíncrono
pipeline.Add<MyOperationAsync>();
```

## Pacote
Um pacote (mensagem) passa pelo tubo (pipe) e aplicamos diversos filtros (operations) neste pacote. Um pacote pode conter diversos conteúdos anexados. Todo pipeline cria um pacote padrão, caso não seja fornecido.

### Criando Pacote com Conteúdo
```csharp
var message = new PipelineMessage();
message.AddContent(new { id = 1 });
```

### Executando com Pacote
```csharp
pipeline.Execute(message);
```

### Manipulando Conteúdo na Operação
```csharp
pipeline.Add(input =>
{
    string param = input.GetContent<string>(); // obter conteúdo
    input.AddContent($"Test 1 - {param}"); // adicionar conteúdo
    if (input.HasContent<string>()) {} // verifica se tem conteúdo
});
```

### Manipulando Conteúdo com Chave na Operação
```csharp
pipeline.Add(input =>
{
    string param = input.GetContent<string>("key"); // obter conteúdo com chave
    input.AddContent("key", $"Test 1 - {param}"); // adicionar conteúdo com chave
    if (input.HasContent("key")) {} // verifica se tem conteúdo com chave
});
```

### Capturando Pacote
```csharp
// obter pacote após execução
IPipelineMessage result = pipeline.GetMessage();
```

### Fechando o Pacote
```csharp
pipeline.Add(input =>
{ 
    input.SetLock(); // bloquear pacote/mensagem
    input.SetFailure(); // registrar falha
});
```

## Funções

### Executando o Pipeline
```csharp
var pipeline = serviceProvider.GetService<IPipeline>(); // async => IPipelineAsync

// executar pipeline
pipeline.Execute(); // async => ExecuteAsync
```

### Configurando Interceptadores
```csharp
// adicionando interceptadores
pipeline.AddInterceptors(_ =>
{
    // ... comandos
}, PipelineInterceptorType.PostOperation); //  PostOperation, PreOperation, Locked, Faulty, FirstOperation, LastOperation

// adicionando interceptadores condicionais
pipeline.AddInterceptors(_ =>
{
    // ... comandos
},
input =>
{
    return input.HasContent<int>();
});

// adicionando interceptadores como eventos
pipeline.AddInterceptors((input, e) =>
{
    // ... comandos
}, PipelineInterceptorType.PostOperation); //  PostOperation, PreOperation, Locked, Faulty, FirstOperation, LastOperation

// adicionando interceptadores condicionais como eventos
pipeline.AddInterceptors((input, e) =>
{
    // ... comandos
},
input =>
{
    return input.HasContent<int>();s
});

```

### Criando Construtores
Você poderá adicionar operações dinâmicas usando um padrão de construção (builder). Geralmente, usamos ao implementar arquiteturas Ports And Adapters onde encaixamos adaptadores que implementam regras especializadas.

#### Construtores Síncronos
```csharp
/// ..my-core/contract/builders/IProductCategoryListBuilder.cs
public interface IProductCategoryListBuilder : IPipelineBuilder { }

/// ..my-adapter-application/application/builders/ProductCategoryListBuilder.cs
public class ProductCategoryListBuilder : IProductCategoryListBuilder
{
    public IPipeline Builder(IPipeline pipeline)
    {
        return pipeline
            .Add<ProductCategoryFileOperation>()
            .Add<ProductCategoryResponseMapperOperation>();
    }
}

/// Program.cs
builder.Services.AddScoped<IProductCategoryListBuilder, ProductCategoryListBuilder>();

/// ..my-application/application/services/MyService.cs /MyMethod
var pipeline = serviceProvider.GetService<IPipeline>();
var builder = serviceProvider.GetService<IProductCategoryListBuilder>();
builder.Builder(pipeline);
```

#### Construtores Assíncronos
```csharp
/// ..my-core/contract/builders/IProductCategoryListBuilderAsync.cs
public interface IProductCategoryListBuilderAsync : IPipelineBuilderAsync { }

/// ..my-adapter-application/application/builders/ProductCategoryListBuilderAsync.cs
public class ProductCategoryListBuilderAsync : IProductCategoryListBuilderAsync
{
    public IPipelineAsync Builder(IPipelineAsync pipeline)
    {
        return pipeline
            .Add<ProductCategoryFileOperationAsync>()
            .Add<ProductCategoryResponseMapperOperationAsync>();
    }
}

/// Program.cs
builder.Services.AddScoped<IProductCategoryListBuilderAsync, ProductCategoryListBuilderAsync>();

/// ..my-application/application/services/MyService.cs /MyMethod
var pipeline = serviceProvider.GetService<IPipelineAsync>();
var builder = serviceProvider.GetService<IProductCategoryListBuilderAsync>();
builder.Builder(pipeline);
```

---

## Pipeline Avançado

O Pipeline Mvp24Hours suporta padrões avançados para workflows complexos:

### Pipeline Tipado

Use `IPipeline<TInput, TOutput>` para pipelines fortemente tipados:

```csharp
// Definir pipeline tipado
public interface IOrderProcessingPipeline : IPipelineAsync<OrderRequest, OrderResult> { }

// Implementação
public class OrderProcessingPipeline : PipelineAsync<OrderRequest, OrderResult>, IOrderProcessingPipeline
{
    public OrderProcessingPipeline()
    {
        Add<ValidateOrderOperation>();
        Add<ProcessPaymentOperation>();
        Add<CreateShipmentOperation>();
        Add<SendNotificationOperation>();
    }
}

// Uso
var result = await pipeline.ExecuteAsync(new OrderRequest { ... });
```

### Middleware Pattern

```csharp
pipeline.UseMiddleware<LoggingMiddleware>();
pipeline.UseMiddleware<TransactionMiddleware>();
pipeline.UseMiddleware<ValidationMiddleware>();
```

### Padrão Fork/Join

Execute operações paralelas e junte os resultados:

```csharp
pipeline.Fork(
    branch1 => branch1.Add<FetchInventoryOperation>(),
    branch2 => branch2.Add<FetchPricingOperation>(),
    branch3 => branch3.Add<FetchPromotionsOperation>()
)
.Join<MergeProductDataOperation>();
```

### Checkpoint/Resume

Para pipelines de longa execução:

```csharp
builder.Services.AddMvp24HoursPipelineAsync(options =>
{
    options.EnableCheckpoints = true;
    options.CheckpointStore = new RedisCheckpointStore(redisConnection);
});

// Na operação
public override async Task ExecuteAsync(IPipelineMessage input)
{
    // Lógica da operação
    await input.SaveCheckpointAsync("step-1-completed");
}

// Retomar do checkpoint
await pipeline.ResumeFromCheckpointAsync(pipelineId);
```

### Resiliência

Retry e circuit breaker integrados:

```csharp
pipeline.Add<ExternalServiceOperation>()
    .WithRetry(maxAttempts: 3, delay: TimeSpan.FromSeconds(1))
    .WithCircuitBreaker(failureThreshold: 5, breakDuration: TimeSpan.FromMinutes(1));
```

### Observabilidade

Integração com OpenTelemetry:

```csharp
builder.Services.AddMvp24HoursPipelineAsync(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.ActivitySourceName = "MyApp.Pipeline";
});
```

---

## Consulte Também

- [Documentação Avançada de Pipeline](pipeline-advanced.md) - Guia completo de funcionalidades avançadas
- [Saga Pattern](cqrs/saga/home.md) - Transações distribuídas
- [CQRS Pipeline Behaviors](cqrs/behaviors/home.md) - Middleware de Command/Query