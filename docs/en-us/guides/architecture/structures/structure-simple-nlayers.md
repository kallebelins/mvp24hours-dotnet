---
templateId: simple-nlayers
tier: Simple
shape: structure
layers: [Core, Application, Infrastructure, WebAPI, Tests]
dependencyRule: Core <- Application <- Infrastructure; WebAPI is composition root
samplePath: samples/src/simple-crud-ef-customer-api
mvp24hoursModules: [core, application-services, database, webapi, observability]
---

---
templateId: simple-nlayers
tier: Simple
shape: Simple N-Layers
dependencyRule: WebAPI -> Core + Infrastructure; Core has no outward project references
samplePath: samples/src/simple-crud-ef-customer-api
mvp24hoursModules:
  - webapi
  - database
  - application-services
layers:
  - Core
  - Infrastructure
  - WebAPI
  - Tests
---

# Simple N-Layers Structure

Use a small set of projects when business rules need separation from persistence and hosting, but a full enterprise decomposition would add noise.

```text
Solution/
├── Product.Core/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Specifications/
│   └── Contracts/
├── Product.Infrastructure/
│   ├── Data/
│   └── Integrations/
├── Product.Application/
│   ├── Services/
│   ├── DTOs/
│   └── Validation/
├── Product.WebAPI/
│   ├── Program.cs
│   └── Endpoints/
└── Product.Tests/
```

Keep Core independent. Application may depend on Core. Infrastructure implements Core/Application contracts. The Web API is the composition root and may reference the projects needed for registration.

Do not add a legacy `Startup.cs`, a mandatory NLog layer, or hand-written middleware when current Mvp24Hours Web API and observability modules already own the concern.

See [Application Services](../../../application-services.md), [Database Context](../../../database/use-context.md), [Web API](../../../webapi.md), and [Observability](../../../observability/home.md).
