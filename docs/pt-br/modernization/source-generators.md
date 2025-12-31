# Source Generators

## Visão Geral

Source generators são um recurso do compilador C# que permite gerar código em tempo de compilação, eliminando abordagens baseadas em reflexão e habilitando:

- **Melhor Performance**: Sem overhead de reflexão em runtime
- **Compatibilidade com Native AOT**: Suporte completo à compilação ahead-of-time
- **Aplicação Menor**: Código amigável para trimming
- **Inicialização Mais Rápida**: Sem compilação JIT para código gerado
- **Validação em Tempo de Compilação**: Erros detectados no build

## Principais Recursos de Source Generators no Mvp24Hours

### 1. Serialização JSON com `[JsonSerializable]`

O framework fornece contextos de serialização JSON gerados por source generators para serialização compatível com AOT:

```csharp
// Use o contexto embutido
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.Default.BusinessResultOfString);

// Ou com as opções padrão
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.DefaultOptions);
```

#### Criando Contextos Customizados

```csharp
// Para seus tipos de domínio
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(List<OrderItem>))]
public partial class DomainJsonContext : JsonSerializerContext { }

// Registrar no DI
services.AddMvp24HoursJsonSourceGeneration()
        .AddJsonSerializerContext(DomainJsonContext.Default);
```

#### Integração com MVC

```csharp
services.AddControllers()
        .AddMvp24HoursJsonSourceGeneration();

// Com contextos customizados
services.AddControllers()
        .AddMvp24HoursJsonSourceGeneration(DomainJsonContext.Default);
```

### 2. Logging de Alta Performance com `[LoggerMessage]`

O framework fornece mensagens de logger geradas por source generators para logging sem alocação:

```csharp
// Ao invés de interpolação de string (aloca memória):
_logger.LogInformation($"Operation {operationName} completed in {elapsed}ms");

// Use métodos gerados (alocação zero):
CoreLoggerMessages.OperationCompleted(_logger, operationName, elapsed);
```

#### Classes de Logger Messages Disponíveis

| Módulo | Classe | Faixa de Event ID |
|--------|--------|-------------------|
| Core | `CoreLoggerMessages` | 1000-1999 |
| Pipeline | `PipelineLoggerMessages` | 2000-2999 |
| CQRS | `CqrsLoggerMessages` | 3000-3999 |
| RabbitMQ | `RabbitMQLoggerMessages` | 4000-4999 |
| EFCore | `EFCoreLoggerMessages` | 5000-5999 |
| WebAPI | `WebAPILoggerMessages` | 6000-6999 |

#### Exemplos de Uso

```csharp
using Mvp24Hours.Core.Logging;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public async Task ProcessOrderAsync(Order order)
    {
        var sw = Stopwatch.StartNew();
        
        CoreLoggerMessages.OperationStarted(_logger, "ProcessOrder", order.Id.ToString());
        
        try
        {
            // Processar pedido...
            
            CoreLoggerMessages.OperationCompleted(_logger, "ProcessOrder", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            CoreLoggerMessages.OperationFailed(_logger, ex, "ProcessOrder", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 3. JsonSerializerContexts Disponíveis

| Contexto | Módulo | Tipos Incluídos |
|----------|--------|-----------------|
| `Mvp24HoursJsonSerializerContext` | Core | BusinessResult, MessageResult, PagingResult, VoidResult |
| `CqrsJsonSerializerContext` | CQRS | DomainEventBase, IntegrationEventBase, SagaState, ScheduledCommand |
| `RabbitMQJsonSerializerContext` | RabbitMQ | MessageEnvelope, Response<T>, ScheduledMessage |

## Compatibilidade com Native AOT

### Verificando Modo AOT

```csharp
using Mvp24Hours.Core.Serialization.SourceGeneration;

if (AotCompatibility.IsNativeAot)
{
    // Executando em modo Native AOT
}
```

### Métodos Compatíveis com AOT

Métodos marcados com `[AotCompatible]` são seguros para uso em Native AOT:

```csharp
[AotCompatible]
public void SafeMethod()
{
    // Usa apenas código gerado por source generators
}
```

### Métodos que Requerem Reflexão

Métodos marcados com `[RequiresReflection]` podem não funcionar em Native AOT:

```csharp
[RequiresReflection("Usa Activator.CreateInstance", Alternative = "Use padrão factory")]
public T CreateInstance<T>()
{
    return Activator.CreateInstance<T>();
}
```

## Configuração

### Habilitando Source Generation

```csharp
// Em Program.cs ou Startup.cs
services.AddMvp24HoursJsonSourceGeneration();

// Com configuração customizada
services.AddMvp24HoursJsonSourceGeneration(options =>
{
    options.WriteIndented = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
```

### Combinando Múltiplos Contextos

```csharp
services.AddMvp24HoursJsonSourceGeneration()
        .AddJsonSerializerContext(CqrsJsonSerializerContext.Default)
        .AddJsonSerializerContext(RabbitMQJsonSerializerContext.Default)
        .AddJsonSerializerContext(DomainJsonContext.Default);
```

## Comparação de Performance

| Abordagem | Alocação | Tempo de Inicialização | Compatível com AOT |
|-----------|----------|------------------------|-------------------|
| JSON baseado em reflexão | ~5KB/operação | Lento | ❌ |
| JSON source-generated | ~0 | Rápido | ✅ |
| Logging interpolação de string | ~200B/log | - | ✅ |
| Logging LoggerMessage | ~0 | - | ✅ |

## Melhores Práticas

1. **Sempre use JSON source-generated** para tipos que são serializados frequentemente
2. **Use LoggerMessage** para caminhos críticos e logging de alta frequência
3. **Crie contextos específicos do domínio** para os tipos da sua aplicação
4. **Marque código incompatível com AOT** com atributos apropriados
5. **Teste com Native AOT** antes de fazer deploy em produção

## Guia de Migração

### De Newtonsoft.Json

```csharp
// Antes (Newtonsoft.Json)
var json = JsonConvert.SerializeObject(result, JsonHelper.JsonDefaultSettings);

// Depois (Source-generated)
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.DefaultOptions);
```

### De ILogger com Interpolação de String

```csharp
// Antes (aloca)
_logger.LogInformation($"Command {commandName} completed in {elapsed}ms");

// Depois (alocação zero)
CqrsLoggerMessages.CommandCompleted(_logger, commandName, elapsed, true);
```

## Documentação Relacionada

- [Funcionalidades do .NET 9](dotnet9-features.md)
- [Guia de Migração](migration-guide.md)
- [Observabilidade](../observability/home.md)

