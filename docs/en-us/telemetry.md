# Telemetry
Solution created to track all application execution levels. You can inject actions for processing using any log manager, including metrics and trace.

## Settings
```csharp

/// Startup.cs
Logger logger = LogManager.GetCurrentClassLogger(); // any log manager

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

// ignore events
services.AddMvp24HoursTelemetryIgnore("rabbitmq-consumer-basic");

```

## Run
```csharp
/// MyFile.cs
TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{tokenDefault}");

```