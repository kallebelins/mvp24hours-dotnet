# Documentation
The habit of documenting interfaces and data classes (value objects, dtos, entities, ...) can help to facilitate code maintenance.

## Native OpenAPI (.NET 9+)

> ⚠️ **Note:** The Swashbuckle-based `AddMvp24HoursWebSwagger`/`AddMvp24HoursSwaggerWithVersioning`
> APIs (and `UseMvp24HoursSwagger`/`UseMvp24HoursSwaggerWithVersioning`/`UseMvp24HoursReDoc`) were
> **removed**. Use Native OpenAPI, documented below. See
> [Migration guide → Swashbuckle-based Swagger APIs removed](migration.md) if you were still on the
> old API.

.NET 9 introduces native OpenAPI support via `Microsoft.AspNetCore.OpenApi`, providing a lightweight, AOT-compatible, first-party alternative for documenting your RESTful API.

### Benefits Over Third-Party OpenAPI Generators

| Feature | Native OpenAPI | Third-party generators |
|---------|---------------|-------------|
| AOT Compatibility | ✅ Full support | ⚠️ Limited |
| Package Size | ~50KB | ~500KB |
| First-party Support | ✅ Microsoft | ❌ Third-party |
| Performance | ✅ Optimized | ⚠️ Reflection-heavy |

### Setup

```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI
```

### Basic Configuration

```csharp
/// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add native OpenAPI with minimal configuration
builder.Services.AddMvp24HoursNativeOpenApiMinimal("My API", "1.0.0");

var app = builder.Build();

// Map OpenAPI endpoints
app.MapMvp24HoursNativeOpenApi();

app.Run();
```

### Full Configuration

```csharp
/// Program.cs
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.Description = "A sample API using native OpenAPI";
    
    // Enable Swagger UI and ReDoc
    options.EnableSwaggerUI = true;
    options.EnableReDoc = true;
    
    // Authentication
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
    options.BearerSecurityScheme = new OpenApiBearerSecurityScheme
    {
        Description = "Enter your JWT token",
        BearerFormat = "JWT"
    };
});

var app = builder.Build();

app.MapMvp24HoursNativeOpenApi();
```

> 📚 For complete documentation on Native OpenAPI, including versioning, document transformers, and advanced features, see [Native OpenAPI Documentation](modernization/native-openapi.md).
