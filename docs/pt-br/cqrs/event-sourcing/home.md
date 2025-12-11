# Event Sourcing - Visão Geral

## O que é Event Sourcing?

Event Sourcing é um padrão arquitetural onde o estado da aplicação é determinado por uma sequência de eventos, ao invés de armazenar apenas o estado atual.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Tradicional vs Event Sourcing                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Tradicional (Estado Atual):                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Order { Id: 1, Status: "Shipped", Total: 100.00 }       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Event Sourcing (Histórico de Eventos):                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 1. OrderCreated { Id: 1, Total: 100.00 }                │   │
│  │ 2. OrderPaid { Id: 1, PaymentId: "PAY-123" }            │   │
│  │ 3. OrderShipped { Id: 1, TrackingNumber: "TRACK-456" }  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Benefícios

| Benefício | Descrição |
|-----------|-----------|
| **Auditoria Completa** | Histórico completo de todas as mudanças |
| **Debugging** | Reproduzir problemas reconstruindo estado |
| **Projeções Flexíveis** | Criar múltiplas visões dos mesmos dados |
| **Time Travel** | Consultar estado em qualquer ponto no tempo |
| **Event Replay** | Reconstruir projeções a partir dos eventos |

## Conceitos Principais

### Evento

Representa algo que aconteceu no passado. Imutável.

```csharp
public record OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Agregado

Entidade que mantém estado e emite eventos.

```csharp
public class Order : EventSourcedAggregate
{
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Reconstruir a partir de eventos
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;
            case OrderPaidEvent:
                Status = OrderStatus.Paid;
                break;
            case OrderShippedEvent:
                Status = OrderStatus.Shipped;
                break;
        }
    }
}
```

### Event Store

Armazena eventos de forma persistente.

```csharp
public interface IEventStore
{
    Task AppendEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, long expectedVersion);
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, long fromVersion = 0);
    Task<long> GetCurrentVersionAsync(Guid aggregateId);
}
```

### Projeção

Visão materializada dos dados para consultas.

```csharp
public class OrderSummaryProjection : IProjection
{
    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                // Inserir novo registro na tabela de consulta
                break;
            case OrderShippedEvent e:
                // Atualizar status na tabela de consulta
                break;
        }
    }
}
```

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                    Event Sourcing Architecture                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Command ──▶ Aggregate ──▶ Events ──▶ Event Store             │
│                                             │                   │
│                                             ▼                   │
│                                      ┌──────────────┐          │
│                                      │  Projections │          │
│                                      └──────────────┘          │
│                                             │                   │
│                                             ▼                   │
│   Query ◀────────────────────────── Read Models               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Fluxo de Operações

### Escrita (Command)

```
1. Carregar eventos do agregado
2. Reconstruir estado aplicando eventos
3. Executar lógica de negócio
4. Emitir novos eventos
5. Persistir eventos no Event Store
6. Publicar eventos para projeções
```

### Leitura (Query)

```
1. Consultar Read Model (projeção)
2. Retornar dados otimizados para consulta
```

## Quando Usar

### ✅ Usar Event Sourcing quando:

- Auditoria completa é requisito
- Domínio complexo com regras de negócio sofisticadas
- Necessidade de múltiplas visões dos dados
- Debugging e troubleshooting são críticos
- Integração entre sistemas via eventos

### ❌ Evitar Event Sourcing quando:

- CRUD simples sem regras complexas
- Requisitos de auditoria básicos
- Time-to-market muito apertado
- Equipe sem experiência no padrão

## Próximos Passos

- [Agregados](event-sourcing/aggregate.md) - Implementando agregados
- [Event Store](event-sourcing/event-store.md) - Armazenamento de eventos
- [Projeções](event-sourcing/projections.md) - Read Models
- [Snapshots](event-sourcing/snapshots.md) - Otimização de performance

