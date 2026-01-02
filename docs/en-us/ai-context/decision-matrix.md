# Decision Matrix for AI Agents

> **AI Agent Instruction**: Use this decision matrix to select the appropriate architecture template and patterns based on project requirements.

---

## Architecture Template Selection

### Quick Decision Tree

```
Is this a simple microservice or rapid prototype?
├── YES → Use Minimal API Template
└── NO
    ├── Does it require complex business rules?
    │   ├── YES → Use Complex N-Layers Template
    │   └── NO → Use Simple N-Layers Template
    └── Does it need external integrations (Ports & Adapters)?
        ├── YES → Use Complex Pipeline Ports & Adapters Template
        └── NO → Use standard template based on complexity
```

### Template Selection Matrix

| Requirement | Minimal API | Simple N-Layers | Complex N-Layers |
|-------------|:-----------:|:---------------:|:----------------:|
| Simple CRUD operations | ✅ | ✅ | ✅ |
| Complex business rules | ❌ | ⚠️ | ✅ |
| Multiple related entities | ⚠️ | ✅ | ✅ |
| Data validation (FluentValidation) | ✅ | ✅ | ✅ |
| Specification pattern | ❌ | ⚠️ | ✅ |
| Service layer abstraction | ❌ | ✅ | ✅ |
| DTOs separation | ⚠️ | ✅ | ✅ |
| AutoMapper | ⚠️ | ✅ | ✅ |
| Unit testing | ⚠️ | ✅ | ✅ |
| High security requirements | ❌ | ⚠️ | ✅ |
| Multiple consumers (API, Services) | ❌ | ✅ | ✅ |
| DDD concepts | ❌ | ⚠️ | ✅ |

**Legend**: ✅ Recommended | ⚠️ Possible but limited | ❌ Not recommended

---

## Database Selection

### Quick Decision Tree

```
What type of data will you store?
├── Structured data with relationships
│   ├── High read performance needed → SQL Server + Dapper (queries)
│   ├── Standard CRUD → SQL Server / PostgreSQL / MySQL + EF
│   └── Need transactions → SQL Server / PostgreSQL + EF + UnitOfWork
├── Document-based (flexible schema)
│   └── Use MongoDB
├── Key-value / Cache
│   └── Use Redis
└── Mixed requirements
    └── Use Hybrid (EF + Dapper) or (EF + MongoDB)
```

### Database Selection Matrix

| Requirement | SQL Server | PostgreSQL | MySQL | MongoDB | Redis |
|-------------|:----------:|:----------:|:-----:|:-------:|:-----:|
| ACID transactions | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| Complex queries | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| JSON support | ✅ | ✅ | ✅ | ✅ | ✅ |
| Full-text search | ✅ | ✅ | ✅ | ✅ | ❌ |
| Horizontal scaling | ⚠️ | ⚠️ | ⚠️ | ✅ | ✅ |
| High write throughput | ⚠️ | ✅ | ⚠️ | ✅ | ✅ |
| Flexible schema | ❌ | ⚠️ | ❌ | ✅ | ✅ |
| Real-time data | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ✅ |
| Cache/Session | ❌ | ❌ | ❌ | ⚠️ | ✅ |
| Relationships | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| Cost (cloud) | $$$ | $$ | $ | $$ | $$ |

### Database Package Selection

| Database | NuGet Package | Mvp24Hours Package |
|----------|--------------|-------------------|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` | `Mvp24Hours.Infrastructure.Data.EFCore` |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | `Mvp24Hours.Infrastructure.Data.EFCore` |
| MySQL | `MySql.EntityFrameworkCore` | `Mvp24Hours.Infrastructure.Data.EFCore` |
| MongoDB | `MongoDB.Driver` | `Mvp24Hours.Infrastructure.Data.MongoDb` |
| Redis | `StackExchange.Redis` | `Mvp24Hours.Infrastructure.Caching.Redis` |
| Dapper | `Dapper` | - (use with EFCore) |

---

## Messaging Pattern Selection

### Quick Decision Tree

```
Does the application need asynchronous processing?
├── NO → Use synchronous API calls
└── YES
    ├── Need guaranteed delivery?
    │   └── Use RabbitMQ with Outbox pattern
    ├── Simple background processing?
    │   └── Use HostedService
    └── Complex workflows?
        └── Use RabbitMQ + Pipeline pattern
```

### Messaging Selection Matrix

| Requirement | Direct API | RabbitMQ | Hosted Service |
|-------------|:----------:|:--------:|:--------------:|
| Synchronous response | ✅ | ❌ | ❌ |
| Fire and forget | ❌ | ✅ | ✅ |
| Guaranteed delivery | ✅ | ✅ | ⚠️ |
| Retry mechanism | ⚠️ | ✅ | ⚠️ |
| Load balancing | ❌ | ✅ | ❌ |
| Multiple consumers | ❌ | ✅ | ❌ |
| Background processing | ❌ | ✅ | ✅ |
| Long-running tasks | ❌ | ✅ | ✅ |
| Real-time updates | ✅ | ⚠️ | ❌ |

---

## Pattern Selection

### Validation Pattern

| Scenario | Recommended Pattern |
|----------|-------------------|
| Simple property validation | Data Annotations |
| Complex business validation | FluentValidation |
| Cross-field validation | FluentValidation |
| Async validation | FluentValidation |
| Localized messages | FluentValidation with Resources |

### Repository Pattern Usage

| Scenario | Implementation |
|----------|---------------|
| Simple CRUD | `IRepository<T>` |
| Async operations | `IRepositoryAsync<T>` |
| Custom queries | `ISpecificationQuery<T>` |
| Optimized reads | Dapper + Custom Repository |
| Caching | Repository + Redis |

### Service Layer Pattern

| Scenario | Implementation |
|----------|---------------|
| Simple operations | Direct Repository access |
| Business logic | `RepositoryService<T>` |
| Paging support | `RepositoryPagingService<T>` |
| Complex operations | Custom Service + `IBusinessResult<T>` |

---

## Architecture Pattern Combinations

### Recommended Combinations

#### 1. Simple REST API (Low Complexity)

```
Template: Minimal API
Database: PostgreSQL + EF
Validation: FluentValidation
Logging: NLog
Health Checks: Yes
```

#### 2. Standard Business Application (Medium Complexity)

```
Template: Simple N-Layers
Database: SQL Server + EF
Validation: FluentValidation
Mapping: AutoMapper
Logging: NLog
Health Checks: Yes
Swagger: Yes
```

#### 3. Enterprise Application (High Complexity)

```
Template: Complex N-Layers
Database: SQL Server + EF + Dapper
Validation: FluentValidation
Mapping: AutoMapper
Patterns: Specification, UnitOfWork, Repository
Logging: NLog + OpenTelemetry
Health Checks: Yes
Swagger: Yes
```

#### 4. Event-Driven Application

```
Template: Complex N-Layers
Database: PostgreSQL + EF
Messaging: RabbitMQ
Patterns: Pipeline, Outbox
Validation: FluentValidation
Logging: NLog + OpenTelemetry
Health Checks: Yes
```

#### 5. High-Performance Read Application

```
Template: Complex N-Layers
Database: SQL Server + EF (writes) + Dapper (reads)
Cache: Redis
Validation: FluentValidation
Mapping: AutoMapper
Logging: NLog
```

#### 6. Document-Based Application

```
Template: Simple N-Layers
Database: MongoDB
Validation: FluentValidation
Mapping: AutoMapper
Logging: NLog
Health Checks: Yes
```

---

## Modernization Options (.NET 9)

### When to Apply Modern Patterns

| Feature | Apply When |
|---------|-----------|
| HybridCache | Distributed caching with L1/L2 |
| Rate Limiting | API throttling needed |
| HTTP Resilience | External API calls |
| Minimal APIs | Simple endpoints, microservices |
| Output Caching | Response caching needed |
| Keyed Services | Multiple implementations |
| ProblemDetails | Standardized error responses |

---

## AI Agent Checklist

Before generating code, verify:

- [ ] Template selected based on complexity
- [ ] Database selected based on data requirements
- [ ] Messaging pattern selected (if async needed)
- [ ] Validation pattern selected
- [ ] Logging strategy defined
- [ ] Health checks identified
- [ ] Modernization features considered
- [ ] All required NuGet packages identified

---

## Advanced Template Selection

### When to Use Advanced Templates

| Template | Use When | Complexity |
|----------|----------|------------|
| **CQRS** | Read/write separation, different models for queries and commands | High |
| **Event-Driven** | Audit requirements, event sourcing, async communication | High |
| **Hexagonal** | Need to swap infrastructure easily, external integrations | High |
| **Clean Architecture** | Domain-centric design, enterprise applications | High |
| **DDD** | Complex business rules, rich domain model | Very High |
| **Microservices** | Independent deployments, team autonomy, scalability | Very High |

### Advanced Template Decision Tree

```
Is the business domain highly complex with many rules?
├── YES
│   ├── Need to track all state changes? → Event-Driven + DDD
│   ├── Multiple bounded contexts? → DDD + Microservices
│   └── Complex queries vs simple commands? → CQRS + DDD
└── NO
    ├── Need to swap external systems easily? → Hexagonal
    ├── Multiple teams working independently? → Microservices
    └── Use Complex N-Layers Template
```

### Advanced Template Selection Matrix

| Requirement | CQRS | Event-Driven | Hexagonal | Clean | DDD | Microservices |
|-------------|:----:|:------------:|:---------:|:-----:|:---:|:-------------:|
| Scalability | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ | ✅ |
| Audit/History | ⚠️ | ✅ | ❌ | ⚠️ | ✅ | ⚠️ |
| Team Autonomy | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ✅ |
| Complex Domain | ⚠️ | ✅ | ⚠️ | ✅ | ✅ | ⚠️ |
| External Integrations | ⚠️ | ⚠️ | ✅ | ✅ | ⚠️ | ✅ |
| Testability | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Learning Curve | Medium | High | Medium | High | Very High | High |

**Legend**: ✅ Excellent fit | ⚠️ Possible | ❌ Not recommended

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Database Patterns](database-patterns.md)
- [Messaging Patterns](messaging-patterns.md)
- [Observability Patterns](observability-patterns.md)
- [Modernization Patterns](modernization-patterns.md)

### Advanced Templates

- [CQRS Template](template-cqrs.md)
- [Event-Driven Template](template-event-driven.md)
- [Hexagonal Template](template-hexagonal.md)
- [Clean Architecture Template](template-clean-architecture.md)
- [DDD Template](template-ddd.md)
- [Microservices Template](template-microservices.md)

### Complementary Documentation

- [Testing Patterns](testing-patterns.md)
- [Security Patterns](security-patterns.md)
- [Error Handling Patterns](error-handling-patterns.md)
- [API Versioning Patterns](api-versioning-patterns.md)
- [Containerization Patterns](containerization-patterns.md)

