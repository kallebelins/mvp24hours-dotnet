# Complex Saga Customer Onboarding API

Demonstrates the **Saga Orchestration** pattern with automatic **compensation** using `Mvp24Hours.Infrastructure.Cqrs`.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- `SagaBase<TData>` with ordered steps and compensating actions on failure
- `ISagaOrchestrator` with in-memory state store (teaching scope)
- `simulateGiftFailure` flag to exercise full compensation in one POST
- Native OpenAPI and orchestration-vs-choreography guidance in README

Target: **net10.0** | Language: English

---

## Architecture

- **WebAPI → Application → Domain**
- No Infrastructure project — persistence adapters live in Application (in-memory store); composed at WebAPI
- **Application must not reference WebAPI**

---

## Pattern Choice: Orchestration (not Choreography)

This sample uses **orchestration**: a single `OnboardCustomerSaga` class (the orchestrator) owns the saga definition and drives every step. All control flow is in one place, which makes it easy to understand, debug, and extend.

**Choreography** — where each service reacts to events from the previous one — is better for fully decoupled microservices where the saga span is loose and evolves independently. For a single-host teaching sample, orchestration gives a clearer end-to-end trace.

| | Orchestration | Choreography |
|---|---|---|
| Control flow | Central saga class | Distributed event handlers |
| Visibility | Easy — one place | Hard — implicit across handlers |
| Coupling | Orchestrator knows all steps | Steps are decoupled |
| Best for | Single host / teaching | Independent microservices |

---

## Flow

```
POST /api/onboarding
  ┌─ ISagaOrchestrator.ExecuteAsync<OnboardCustomerSaga, OnboardCustomerData> ─────┐
  │                                                                                  │
  │  Step 1 — CreateCustomerStep  (Order = 1, CanCompensate = true)                 │
  │    Execute:   persist Customer to in-memory store; set data.CustomerId           │
  │    Compensate: delete Customer by data.CustomerId                                │
  │                                                                                  │
  │  Step 2 — ReserveWelcomeGiftStep  (Order = 2, CanCompensate = true)             │
  │    Execute:   simulate gift-service call; set data.WelcomeGiftCode               │
  │              (throws InvalidOperationException if SimulateGiftFailure = true)    │
  │    Compensate: clear data.WelcomeGiftCode (release reservation)                 │
  │                                                                                  │
  │  Step 3 — SendWelcomeEmailStep  (Order = 3, CanCompensate = false)              │
  │    Execute:   simulate e-mail send; set data.WelcomeEmailSent = true             │
  │    Compensate: no-op — e-mail cannot be unsent                                  │
  │                                                                                  │
  │  On success → SagaStatus.Completed                                               │
  │  On failure at Step 2 → compensate Step 1 → SagaStatus.Compensated              │
  └──────────────────────────────────────────────────────────────────────────────────┘
```

### Compensation trace (SimulateGiftFailure = true)

```
→ Step 1 CreateCustomer  ✓  (customer written)
→ Step 2 ReserveWelcomeGift  ✗  (throws)
← Compensate Step 1 CreateCustomer  ✓  (customer deleted)
→ SagaStatus: Compensated
```

---

## Project Layout

```
CustomerAPI.Domain/
  Entities/
    Customer.cs                 — Simple in-memory entity
  Repositories/
    ICustomerRepository.cs      — Port for customer persistence
  Sagas/
    OnboardCustomerData.cs      — Saga data bag (input + step-set fields)

CustomerAPI.Application/
  Repositories/
    InMemoryCustomerRepository.cs  — ConcurrentDictionary adapter
  Sagas/
    OnboardCustomerSaga.cs      — SagaBase<OnboardCustomerData> configuration
    Steps/
      CreateCustomerStep.cs     — ISagaStep: create + compensate
      ReserveWelcomeGiftStep.cs — ISagaStep: gift + compensate; can fail
      SendWelcomeEmailStep.cs   — ISagaStep: email, CanCompensate = false

CustomerAPI.WebAPI/
  Controllers/
    OnboardingController.cs     — POST /api/onboarding, GET /customers, GET /{id}/status
  Extensions/
    ServiceCollectionExtensions.cs — DI wiring
  Program.cs
```

---

## Key APIs Used

| API | Role |
|-----|------|
| `SagaBase<TData>` | Base class for saga; configure steps via `ConfigureSteps(...)` |
| `SagaStepBase<TData>` | Convenience base for steps; override `ExecuteAsync` / `CompensateAsync` |
| `ISagaStep<TData>` | Interface: `Name`, `Order`, `CanCompensate`, `ExecuteAsync`, `CompensateAsync` |
| `ISagaOrchestrator` | Injected into controller; call `ExecuteAsync<TSaga, TData>(data)` |
| `SagaResult<TData>` | Result: `IsSuccess`, `WasCompensated`, `SagaId`, `Data`, `ErrorMessage` |
| `AddSagaOrchestration(...)` | Registers orchestrator + in-memory state store; scans assemblies for sagas |
| `InMemorySagaStateStore` | Default state store; replace with SQL-backed store for production |

---

## Running the Sample

```bash
cd samples/src/complex-saga-rabbitmq-customer-api/CustomerAPI.WebAPI
dotnet run
```

Open Swagger UI at `https://localhost:{port}/swagger`.

### Try — Happy Path

```http
POST /api/onboarding
{
  "name": "Alice",
  "email": "alice@example.com",
  "simulateGiftFailure": false
}
```

Expected response (200):
```json
{
  "sagaId": "...",
  "customerId": "...",
  "welcomeGiftCode": "WELCOME-XXXXXXXXXXXX",
  "welcomeEmailSent": true,
  "status": "Completed"
}
```

### Try — Compensation Path

```http
POST /api/onboarding
{
  "name": "Bob",
  "email": "bob@example.com",
  "simulateGiftFailure": true
}
```

Expected response (422):
```json
{
  "sagaId": "...",
  "error": "...",
  "status": "Compensated",
  "message": "Saga failed and all eligible steps were compensated."
}
```

Verify that Bob is absent from `GET /api/onboarding/customers` — compensation deleted the record.

---

## RabbitMQ — Optional Next Step

This sample deliberately uses **in-memory** storage to keep the compilation barrier low.
To add RabbitMQ event publishing on saga completion:

1. Follow `complex-event-driven-rabbitmq-customer-api` (Phase 5.5) for outbox setup.
2. Override `OnSagaCompletedAsync` in `OnboardCustomerSaga` and publish a `CustomerOnboardedIntegrationEvent` via `IIntegrationEventOutbox`.
3. Add `AspNetCore.HealthChecks.Rabbitmq` and SQL Server outbox table as shown in the 5.5 sample.

---

## References

- [Saga home](../../../docs/en-us/cqrs/saga/home.md), [implementation](../../../docs/en-us/cqrs/saga/implementation.md), [compensation](../../../docs/en-us/cqrs/saga/compensation.md)
- Pattern: [microservices.io/patterns/data/saga.html](https://microservices.io/patterns/data/saga.html)
- Outbox/inbox sibling: [`complex-event-driven-rabbitmq-customer-api`](../complex-event-driven-rabbitmq-customer-api/README.md)

## What this sample intentionally does not cover

- Durable saga state persistence (in-memory store only)
- RabbitMQ choreography across independent services (orchestration in one host)
- Production dead-letter handling beyond the documented optional next steps
