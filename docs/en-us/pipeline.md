# Pipeline (Pipe and Filters Pattern)
It is a design pattern that represents a pipe with several operations (filters), executed sequentially, with the aim of traveling, integrating and/or manipulating a packet/message.

## Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.Pipe -Version 9.0.101
```

## Basic Settings
```csharp
/// Startup.cs
services.AddMvp24HoursPipeline(options => // async => AddMvp24HoursPipelineAsync
{
    options.IsBreakOnFail = false;
});
```

## Settings with Factory
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

## Operations/Filters

### Adding Anonymous
```csharp
// add operation/filter as action
pipeline.Add(_ =>
{
    Trace.WriteLine("Test 1");
});
```

### Adding Instances
To create an operation, simply implement an IOperation or an OperationBase:

#### Synchronous Operations/Filters
```csharp
/// MyOperation.cs
public class MyOperation : OperationBase
{
    public override bool IsRequired => false; // indicates whether the operation will execute even with the package blocked

    public override void Execute(IPipelineMessage input)
    {
        // performs action
        return input;
    }
}

// add to pipeline
pipeline.Add<MyOperation>();
```

#### Asynchronous Operations/Filters
```csharp
/// MyOperationAsync.cs
public class MyOperationAsync : OperationBaseAsync
{
    public override bool IsRequired => false; // indicates whether the operation will execute even with the package blocked

    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        await Task.CompletedTask;
    }
}

// add to async pipeline
pipeline.Add<MyOperationAsync>();
```

## Package
A packet (message) passes through the pipe and we apply several filters (operations) to this packet. A package can contain several attached contents. Every pipeline creates a default package if not provided.

### Creating Package with Content
```csharp
var message = new PipelineMessage();
message.AddContent(new { id = 1 });
```

### Running with Package
```csharp
pipeline.Execute(message);
```

### Manipulating Content in Operation
```csharp
pipeline.Add(input =>
{
    string param = input.GetContent<string>(); // get content
    input.AddContent($"Test 1 - {param}"); // add content
    if (input.HasContent<string>()) {} // check if it has content
});
```

### Manipulating Content with Key in Operation
```csharp
pipeline.Add(input =>
{
    string param = input.GetContent<string>("key"); // get content with key
    input.AddContent("key", $"Test 1 - {param}"); // add content with key
    if (input.HasContent("key")) {} // checks if it has content with key
});
```

### Capturing Packet
```csharp
// get package after execution
IPipelineMessage result = pipeline.GetMessage();
```

### Closing the Package
```csharp
pipeline.Add(input =>
{ 
    input.SetLock(); // block packet/message
    input.SetFailure(); // record failure
});
```

## Functions

### Running the Pipeline
```csharp
var pipeline = serviceProvider.GetService<IPipeline>(); // async => IPipelineAsync

// execute pipeline
pipeline.Execute(); // async => ExecuteAsync
```

### Configuring Interceptors
```csharp
// adding interceptors
pipeline.AddInterceptors(_ =>
{
    // ... commands
}, PipelineInterceptorType.PostOperation); //  PostOperation, PreOperation, Locked, Faulty, FirstOperation, LastOperation

// adding conditional interceptors
pipeline.AddInterceptors(_ =>
{
    // ... commands
},
input =>
{
    return input.HasContent<int>();
});

// adding interceptors as events
pipeline.AddInterceptors((input, e) =>
{
    // ... commands
}, PipelineInterceptorType.PostOperation); //  PostOperation, PreOperation, Locked, Faulty, FirstOperation, LastOperation

// adding conditional interceptors as events
pipeline.AddInterceptors((input, e) =>
{
    // ... commands
},
input =>
{
    return input.HasContent<int>();s
});

```

### Creating Constructors
You can add dynamic operations using a builder pattern. Generally, we use when implementing Ports And Adapters architectures where we fit adapters that implement specialized rules.

#### Synchronous Constructors
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

#### Asynchronous Constructors
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