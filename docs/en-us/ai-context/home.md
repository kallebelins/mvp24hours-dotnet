# AI Context - Architecture Instructions

> **Purpose**: This documentation is designed exclusively for AI agents to generate complete, production-ready .NET architectures using Mvp24Hours framework.

---

## 🚀 Quick Setup - Cursor Rules Files

This documentation includes ready-to-use rule files for Cursor IDE that enable AI agents to follow Mvp24Hours SDK patterns automatically.

### Available Files

| File | Description | Language |
|------|-------------|----------|
| [`llms_complete_en.txt`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/docs/llms_complete_en.txt) | Complete rules with detailed instructions | English |
| [`llms_compact_en.txt`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/docs/llms_compact_en.txt) | Compact rules with keyword index | English |
| [`llms_complete_pt.txt`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/docs/llms_complete_pt.txt) | Complete rules with detailed instructions | Portuguese |
| [`llms_compact_pt.txt`](https://raw.githubusercontent.com/kallebelins/mvp24hours-dotnet/main/docs/llms_compact_pt.txt) | Compact rules with keyword index | Portuguese |

### How to Use with Cursor IDE

#### Method 1: Root `.cursorrules` File (Recommended for single project)

1. Copy the desired file to your project root
2. Rename it to `.cursorrules`

```
your-project/
├── .cursorrules          ← Rename llms_complete_en.txt to .cursorrules
├── src/
└── ...
```

#### Method 2: `.cursor/rules/` Folder (Recommended for multiple rules)

1. Create a `.cursor/rules/` folder in your project root
2. Copy the file and rename with `.mdc` extension

```
your-project/
├── .cursor/
│   └── rules/
│       └── mvp24hours.mdc    ← Rename llms_complete_en.txt to mvp24hours.mdc
├── src/
└── ...
```

### Official Cursor Documentation

- [Cursor Rules](https://docs.cursor.com/context/rules) - How rules work
- [Rules for AI](https://docs.cursor.com/context/rules-for-ai) - Best practices for AI rules

---

## Overview

This section provides structured instructions and templates for AI agents to create .NET applications following best practices and patterns implemented in the Mvp24Hours ecosystem.

### How AI Agents Should Use This Documentation

1. **Analyze Requirements**: Understand the project requirements (database type, complexity, messaging needs)
2. **Select Template**: Use the [Decision Matrix](decision-matrix.md) to choose the appropriate architecture template
3. **Apply Patterns**: Follow the specific patterns for each component (database, messaging, observability)
4. **Generate Structure**: Create the project structure following the conventions defined in [Project Structure](project-structure.md)

---

## Available Architecture Templates

### Basic Templates

| Template | Complexity | Use Case |
|----------|-----------|----------|
| **Minimal API** | Low | Simple CRUD, microservices, rapid prototyping |
| **Simple N-Layers** | Medium | Small to medium applications with basic business rules |
| **Complex N-Layers** | High | Enterprise applications with complex business logic |

### Advanced Templates

| Template | Complexity | Use Case | Documentation |
|----------|-----------|----------|---------------|
| **CQRS** | High | Command/Query separation, read/write models | [template-cqrs.md](template-cqrs.md) |
| **Event-Driven** | High | Audit trails, event sourcing, async communication | [template-event-driven.md](template-event-driven.md) |
| **Hexagonal** | High | External integrations, infrastructure swap | [template-hexagonal.md](template-hexagonal.md) |
| **Clean Architecture** | High | Domain-centric, enterprise applications | [template-clean-architecture.md](template-clean-architecture.md) |
| **DDD** | Very High | Complex business rules, rich domain model | [template-ddd.md](template-ddd.md) |
| **Microservices** | Very High | Independent deployments, team autonomy | [template-microservices.md](template-microservices.md) |

### Template Categories

#### By Database
- **Relational (EF)**: SQL Server, PostgreSQL, MySQL
- **NoSQL (MongoDB)**: Document-based storage
- **Key-Value (Redis)**: Cache and session management
- **Hybrid (EF + Dapper)**: Optimized queries with EF for mutations

#### By Architecture Pattern
- **CRUD**: Standard Create, Read, Update, Delete operations
- **Pipeline**: Pipe and Filters pattern for complex workflows
- **Ports & Adapters**: Hexagonal architecture for high decoupling
- **CQRS**: Separate read and write models
- **Event-Driven**: Domain events, integration events, event sourcing
- **DDD**: Aggregates, value objects, domain services

#### By Messaging
- **Synchronous**: Direct API calls
- **Asynchronous (RabbitMQ)**: Message broker integration with Hosted Services

---

## Quick Reference for AI Agents

### Required Packages (NuGet)

```text
# Core Framework
Mvp24Hours.Core
Mvp24Hours.Application

# Database - Entity Framework
Mvp24Hours.Infrastructure.Data.EFCore
Microsoft.EntityFrameworkCore.SqlServer | Npgsql.EntityFrameworkCore.PostgreSQL | MySql.EntityFrameworkCore

# Database - MongoDB
Mvp24Hours.Infrastructure.Data.MongoDb
MongoDB.Driver

# Database - Redis
Mvp24Hours.Infrastructure.Caching.Redis
StackExchange.Redis

# Messaging - RabbitMQ
Mvp24Hours.Infrastructure.RabbitMQ
RabbitMQ.Client

# Pipeline
Mvp24Hours.Infrastructure.Pipe

# Web API
Mvp24Hours.WebAPI
Swashbuckle.AspNetCore

# Mapping
AutoMapper
AutoMapper.Extensions.Microsoft.DependencyInjection

# Validation
FluentValidation
FluentValidation.AspNetCore

# Logging
NLog.Web.AspNetCore

# Health Checks
AspNetCore.HealthChecks.UI.Client
```

### Standard Project References

```xml
<!-- Core Project -->
<ItemGroup>
  <PackageReference Include="Mvp24Hours.Core" Version="9.*" />
  <PackageReference Include="FluentValidation" Version="11.*" />
</ItemGroup>

<!-- Infrastructure Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
</ItemGroup>

<!-- Application Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  <ProjectReference Include="..\ProjectName.Infrastructure\ProjectName.Infrastructure.csproj" />
  <PackageReference Include="Mvp24Hours.Application" Version="9.*" />
  <PackageReference Include="AutoMapper" Version="12.*" />
</ItemGroup>

<!-- WebAPI Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Application\ProjectName.Application.csproj" />
  <PackageReference Include="Mvp24Hours.WebAPI" Version="9.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
  <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
</ItemGroup>
```

---

## AI Agent Instructions

### When Generating Code, Always:

1. **Follow Mvp24Hours Conventions**
   - Use `IEntityBase<TKey>` for entities with typed ID
   - Use `IEntityLog` for audit fields (Created, Modified, Removed)
   - Use `IValidator<T>` from FluentValidation for business validation
   - Use `IPipelineAsync` for pipeline operations

2. **Apply Dependency Injection**
   - Register services in `ServiceBuilderExtensions`
   - Use `IServiceCollection` extension methods
   - Follow the pattern: `services.Add{ServiceName}()`

3. **Implement Repository Pattern**
   - Use `IRepository<TEntity>` for standard CRUD
   - Use `IRepositoryAsync<TEntity>` for async operations
   - Apply `ISpecificationQuery<TEntity>` for query specifications

4. **Configure Unit of Work**
   - Use `IUnitOfWork` for transaction management
   - Call `SaveChangesAsync()` to persist changes
   - Use `IUnitOfWorkAsync` for async transactions

5. **Handle Responses**
   - Use `IBusinessResult<T>` for service responses
   - Apply `MessageResult` for validation messages
   - Use standard HTTP status codes in controllers

### Code Generation Checklist

- [ ] Solution file (.sln) with correct project references
- [ ] Core layer with entities, DTOs, validators, contracts
- [ ] Infrastructure layer with DbContext, repositories, configurations
- [ ] Application layer with services, mappings, facade
- [ ] WebAPI layer with controllers, Program.cs, Startup.cs
- [ ] appsettings.json with connection strings and configuration
- [ ] Health checks configuration
- [ ] Swagger/OpenAPI documentation
- [ ] NLog configuration
- [ ] Docker support (optional)

---

## Related Documentation

### Core Documentation
- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Messaging Patterns](messaging-patterns.md)
- [Observability Patterns](observability-patterns.md)
- [Modernization Patterns](modernization-patterns.md)
- [Project Structure](project-structure.md)

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

### AI Implementation Templates
- [AI Implementation Index](ai-implementation-index.md) - Overview of AI approaches
- [AI Decision Matrix](ai-decision-matrix.md) - When to use each AI approach

#### Semantic Kernel Templates
- [Chat Completion](template-sk-chat-completion.md) - Basic conversational AI
- [Plugins & Functions](template-sk-plugins.md) - Tool-augmented AI
- [RAG Basic](template-sk-rag-basic.md) - Document Q&A
- [Planners](template-sk-planners.md) - Task decomposition

#### Semantic Kernel Graph Templates
- [Graph Executor](template-skg-graph-executor.md) - Workflow orchestration
- [ReAct Agent](template-skg-react-agent.md) - Reasoning + Acting
- [Chain of Thought](template-skg-chain-of-thought.md) - Step-by-step reasoning
- [Chatbot with Memory](template-skg-chatbot-memory.md) - Contextual conversations
- [Multi-Agent](template-skg-multi-agent.md) - Agent coordination
- [Document Pipeline](template-skg-document-pipeline.md) - Document processing
- [Human-in-the-Loop](template-skg-human-in-loop.md) - Approval workflows
- [Checkpointing](template-skg-checkpointing.md) - State persistence
- [Streaming](template-skg-streaming.md) - Real-time events
- [Observability](template-skg-observability.md) - Metrics and monitoring

#### Microsoft Agent Framework Templates
- [Agent Framework Basic](template-agent-framework-basic.md) - Simple agent setup
- [Graph Workflows](template-agent-framework-workflows.md) - Workflow-based agents
- [Multi-Agent](template-agent-framework-multi-agent.md) - Agent orchestration
- [Middleware](template-agent-framework-middleware.md) - Request/response processing

---

## Sample Projects Repository

All templates are based on real implementations available at:
- **Repository**: [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- **Framework**: [mvp24hours-dotnet](https://github.com/kallebelins/mvp24hours-dotnet)

