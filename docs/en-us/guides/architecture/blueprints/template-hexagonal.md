# Hexagonal Architecture Blueprint

Hexagonal architecture isolates business behavior behind inbound and outbound ports. Use it when external systems change independently or when the same use cases need multiple delivery mechanisms.

```text
HTTP / Worker / Message Consumer
             |
        inbound ports
             |
       Application + Domain
             |
        outbound ports
             |
 EF Core / MongoDB / RabbitMQ / Email
```

Define ports from the perspective of the application, not as wrappers around vendor SDKs. Keep domain types inside the core and map at adapter boundaries. Compose adapters in the host.

Mvp24Hours repositories, mediator requests, infrastructure abstractions, and provider interfaces can implement these boundaries; use their canonical module pages for signatures and registration.

See [Core Abstractions](../../../core/infrastructure-abstractions.md), [Application Services](../../../application-services.md), [Data & Persistence](../../../database/relational.md), and [Infrastructure Modules](../../../infrastructure/home.md).
