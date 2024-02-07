# Telemetry
Solução criada para rastrear todos os níveis de execução da aplicação. Poderá injetar ações para tratamento usando qualquer gerenciador de log, incluindo métricas e trace.

## Configuração
```csharp

/// Startup.cs
Logger logger = LogManager.GetCurrentClassLogger(); // qualquer gerenciador de log

// trace
services.AddMvp24HoursTelemetry(TelemetryLevel.Information | TelemetryLevel.Verbose,
    (name, state) =>
    {
        logger.Trace($"{name}|{string.Join("|", state)}");
    }
);

// erro
services.AddMvp24HoursTelemetry(TelemetryLevel.Error,
    (name, state) =>
    {
        if (name.EndsWith("-failure"))
        {
            logger.Error(state.ElementAtOrDefault(0) as Exception);
        }
        else
        {
            logger.Error($"{name}|{string.Join("|", state)}");
        }
    }
);

// ignorar eventos
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");

```

## Rodar / Executar
```csharp
/// MyFile.cs
TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{tokenDefault}");

```