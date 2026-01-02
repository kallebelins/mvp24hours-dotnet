# Project Structure for AI Agents

> **AI Agent Instruction**: This is the index page for project structures. Choose the appropriate structure based on project complexity and requirements.

---

## Available Structures

| Structure | Complexity | Layers | Best For |
|-----------|------------|--------|----------|
| [Minimal API](structure-minimal-api.md) | Low | 1 | Microservices, simple CRUDs, MVPs |
| [Simple N-Layers](structure-simple-nlayers.md) | Medium | 3 | Medium projects, clear separation |
| [Complex N-Layers](structure-complex-nlayers.md) | High | 4 | Enterprise, complex business logic |

---

## Quick Selection Guide

### Use Minimal API when:
- Building microservices with single responsibility
- Creating simple CRUD APIs (1-5 entities)
- Developing prototypes or MVPs
- Need lightweight, fast deployment
- No complex business logic required

### Use Simple N-Layers when:
- Building medium-sized applications (5-15 entities)
- Need clear separation of concerns
- Multiple developers working on the project
- Project may grow over time
- Business logic in services (not complex pipelines)

### Use Complex N-Layers when:
- Building enterprise applications (15+ entities)
- Complex business logic requiring dedicated application layer
- Using patterns like CQRS, DDD, or Ports & Adapters
- Multiple data sources (EF Core + Dapper)
- Long-term projects with multiple teams

---

## Structure Comparison

```
Minimal API (1 project):
└── ProjectName/
    ├── Entities/
    ├── ValueObjects/
    ├── Validators/
    ├── Data/
    └── Endpoints/

Simple N-Layers (3 projects):
├── ProjectName.Core/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Validators/
├── ProjectName.Infrastructure/
│   └── Data/
└── ProjectName.WebAPI/
    └── Controllers/

Complex N-Layers (4 projects):
├── ProjectName.Core/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Validators/
│   └── Contract/
├── ProjectName.Infrastructure/
│   └── Data/
├── ProjectName.Application/
│   ├── Services/
│   ├── Mappings/
│   └── Pipelines/
└── ProjectName.WebAPI/
    ├── Controllers/
    └── Middlewares/
```

---

## Common Naming Conventions

These conventions apply to all structures:

### Files and Folders

| Type | Convention | Example |
|------|------------|---------|
| Entity | PascalCase, singular | `Customer.cs` |
| DTO | PascalCase + Dto suffix | `CustomerDto.cs` |
| Create DTO | PascalCase + CreateDto | `CustomerCreateDto.cs` |
| Update DTO | PascalCase + UpdateDto | `CustomerUpdateDto.cs` |
| Filter DTO | PascalCase + FilterDto | `CustomerFilterDto.cs` |
| Validator | PascalCase + Validator | `CustomerValidator.cs` |
| Service | PascalCase + Service | `CustomerService.cs` |
| Repository | PascalCase + Repository | `CustomerRepository.cs` |
| Controller | PascalCase + Controller | `CustomerController.cs` |
| Configuration | PascalCase + Configuration | `CustomerConfiguration.cs` |
| Specification | Descriptive + Spec | `CustomerByFilterSpec.cs` |
| Consumer | Message + Consumer | `CustomerCreatedConsumer.cs` |
| Operation | Verb + Entity + Operation | `ValidateCustomerOperation.cs` |
| Profile (AutoMapper) | PascalCase + Profile | `CustomerProfile.cs` |

### Common Namespace Patterns

```csharp
// Core Layer
{ProjectName}.Core.Entities
{ProjectName}.Core.ValueObjects
{ProjectName}.Core.Validators
{ProjectName}.Core.Contract.Services
{ProjectName}.Core.Specifications

// Infrastructure Layer
{ProjectName}.Infrastructure.Data
{ProjectName}.Infrastructure.Data.Configurations

// Application Layer (Complex only)
{ProjectName}.Application.Services
{ProjectName}.Application.Mappings
{ProjectName}.Application.Pipelines

// WebAPI Layer
{ProjectName}.WebAPI.Controllers
{ProjectName}.WebAPI.Extensions
{ProjectName}.WebAPI.Middlewares
```

---

## Configuration Files (All Structures)

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### launchSettings.json

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## Base Entity Classes

All entities should inherit from Mvp24Hours base classes:

```csharp
// For entities with auto-increment integer ID
public class Customer : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
}

// For entities with GUID ID
public class Order : EntityBase<Guid>
{
    public DateTime OrderDate { get; set; }
}

// For entities with audit fields (Created, Modified, etc.)
public class AuditableEntity : EntityBaseLog<int, int>
{
    public string Description { get; set; } = string.Empty;
}
```

---

## DTO Patterns

### Record DTOs (Recommended)

```csharp
// Response DTO
public record CustomerDto(
    int Id,
    string Name,
    string Email,
    bool Active
);

// Create DTO
public record CustomerCreateDto(
    string Name,
    string Email
);

// Update DTO
public record CustomerUpdateDto(
    string Name,
    string Email,
    bool Active
);

// Filter DTO (for queries)
public record CustomerFilterDto(
    string? Name,
    string? Email,
    bool? Active
);
```

### Class DTOs (When mutation is needed)

```csharp
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Messaging Patterns](messaging-patterns.md)
- [Observability Patterns](observability-patterns.md)
