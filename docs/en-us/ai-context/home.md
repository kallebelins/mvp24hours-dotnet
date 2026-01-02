# AI Context - Architecture Instructions

> **Purpose**: This documentation is designed exclusively for AI agents to generate complete, production-ready .NET architectures using Mvp24Hours framework.

---

## Overview

This section provides structured instructions and templates for AI agents to create .NET applications following best practices and patterns implemented in the Mvp24Hours ecosystem.

### How AI Agents Should Use This Documentation

1. **Analyze Requirements**: Understand the project requirements (database type, complexity, messaging needs)
2. **Select Template**: Use the [Decision Matrix](ai-context/decision-matrix.md) to choose the appropriate architecture template
3. **Apply Patterns**: Follow the specific patterns for each component (database, messaging, observability)
4. **Generate Structure**: Create the project structure following the conventions defined in [Project Structure](ai-context/project-structure.md)

---

## Available Architecture Templates

| Template | Complexity | Use Case |
|----------|-----------|----------|
| **Minimal API** | Low | Simple CRUD, microservices, rapid prototyping |
| **Simple N-Layers** | Medium | Small to medium applications with basic business rules |
| **Complex N-Layers** | High | Enterprise applications with complex business logic |

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
  <PackageReference Include="Mvp24Hours.Core" Version="8.*" />
  <PackageReference Include="FluentValidation" Version="11.*" />
</ItemGroup>

<!-- Infrastructure Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="8.*" />
</ItemGroup>

<!-- Application Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  <ProjectReference Include="..\ProjectName.Infrastructure\ProjectName.Infrastructure.csproj" />
  <PackageReference Include="Mvp24Hours.Application" Version="8.*" />
  <PackageReference Include="AutoMapper" Version="12.*" />
</ItemGroup>

<!-- WebAPI Project -->
<ItemGroup>
  <ProjectReference Include="..\ProjectName.Application\ProjectName.Application.csproj" />
  <PackageReference Include="Mvp24Hours.WebAPI" Version="8.*" />
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

- [Architecture Templates](ai-context/architecture-templates.md)
- [Decision Matrix](ai-context/decision-matrix.md)
- [Database Patterns](ai-context/database-patterns.md)
- [Messaging Patterns](ai-context/messaging-patterns.md)
- [Observability Patterns](ai-context/observability-patterns.md)
- [Modernization Patterns](ai-context/modernization-patterns.md)
- [Project Structure](ai-context/project-structure.md)

---

## Sample Projects Repository

All templates are based on real implementations available at:
- **Repository**: [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- **Framework**: [mvp24hours-dotnet](https://github.com/kallebelins/mvp24hours-dotnet)

