# Pipeline (Pipe and Filters Pattern)
É um padrão de projeto que representa um tubo com diversas operações (filtros), executadas de forma sequencial, com o intuito de trafegar, integrar e/ou manipular um pacote/mensagem.

## Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.Pipe -Version 9.0.101
```

## Configuração Básica
```csharp
/// Startup.cs
services.AddMvp24HoursPipeline(options => // async => AddMvp24HoursPipelineAsync
{
    options.IsBreakOnFail = false;
});
```

## Configuração com Factory
```csharp
/// Startup.cs
services.AddMvp24HoursPipeline(factory: (_) => // async => AddMvp24HoursPipelineAsync
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

/// Startup.cs
services.AddScoped<IProductCategoryListBuilder, ProductCategoryListBuilder>();

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

/// Startup.cs
services.AddScoped<IProductCategoryListBuilderAsync, ProductCategoryListBuilderAsync>();

/// ..my-application/application/services/MyService.cs /MyMethod
var pipeline = serviceProvider.GetService<IPipelineAsync>();
var builder = serviceProvider.GetService<IProductCategoryListBuilderAsync>();
builder.Builder(pipeline);
```