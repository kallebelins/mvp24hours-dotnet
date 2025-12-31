# Native OpenAPI (.NET 9)

## Overview

.NET 9 introduces native OpenAPI support via `Microsoft.AspNetCore.OpenApi`, providing a lightweight, AOT-compatible alternative to Swashbuckle for API documentation. Mvp24Hours integrates this native functionality with convenient extension methods and configuration options.

## Benefits Over Swashbuckle

| Feature | Native OpenAPI | Swashbuckle |
|---------|---------------|-------------|
| AOT Compatibility | ✅ Full support | ⚠️ Limited |
| Package Size | ~50KB | ~500KB |
| First-party Support | ✅ Microsoft | ❌ Third-party |
| Performance | ✅ Optimized | ⚠️ Reflection-heavy |
| Document Transformers | ✅ Built-in | ⚠️ Filters/Conventions |

## Installation

Native OpenAPI is included in the Mvp24Hours.WebAPI package. The `Microsoft.AspNetCore.OpenApi` package is automatically referenced.

```xml
<PackageReference Include="Mvp24Hours.WebAPI" Version="9.x.x" />
```

## Basic Configuration

### Minimal Setup

```csharp
// Program.cs
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
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.DocumentName = "v1";
    options.Title = "My API";
    options.Version = "1.0.0";
    options.Description = "A sample API using native OpenAPI";
    
    // Contact information
    options.Contact = new OpenApiContactInfo
    {
        Name = "API Support",
        Email = "support@example.com",
        Url = "https://example.com/support"
    };
    
    // License
    options.License = new OpenApiLicenseInfo
    {
        Name = "MIT",
        Url = "https://opensource.org/licenses/MIT",
        Identifier = "MIT"
    };
    
    // Terms of service
    options.TermsOfServiceUrl = "https://example.com/terms";
    
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
```

## API Versioning Support

### Multiple API Versions

```csharp
builder.Services.AddMvp24HoursNativeOpenApiWithVersions(options =>
{
    options.Title = "My API";
    options.DocumentName = "v1";
    options.Version = "1.0.0";
    
    // Add additional versions
    options.AdditionalVersions.Add(new OpenApiVersionConfig
    {
        DocumentName = "v2",
        Version = "2.0.0",
        Title = "My API v2",
        Description = "Version 2 with new features"
    });
    
    options.AdditionalVersions.Add(new OpenApiVersionConfig
    {
        DocumentName = "v1-deprecated",
        Version = "1.0.0",
        Title = "My API v1 (Deprecated)",
        IsDeprecated = true,
        DeprecationMessage = "Please migrate to v2"
    });
});
```

## Document Transformers

Native OpenAPI uses document transformers for customization. Mvp24Hours provides several built-in transformers.

### Security Scheme Transformer

Automatically added based on `AuthenticationScheme` configuration:

```csharp
options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
// or
options.AuthenticationScheme = OpenApiAuthenticationScheme.ApiKey;
options.ApiKeySecurityScheme = new OpenApiApiKeySecurityScheme
{
    Name = "X-API-Key",
    Location = ApiKeyLocation.Header,
    Description = "API Key authentication"
};
```

### Custom Headers Transformer

Add common headers to all operations:

```csharp
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer(new CustomHeadersTransformer(
        ("X-Correlation-Id", "Correlation ID for request tracing", false),
        ("X-Tenant-Id", "Tenant identifier", true)
    ));
});
```

### Common Responses Transformer

Add standard error responses to all operations:

```csharp
options.AddDocumentTransformer(new CommonResponsesTransformer(
    add401: true,
    add403: true,
    add500: true,
    add503: true
));
```

### ProblemDetails Transformer

Add RFC 7807 ProblemDetails schema to error responses:

```csharp
options.AddDocumentTransformer(new ProblemDetailsTransformer());
```

### Rate Limit Headers Transformer

Add rate limit headers to all responses:

```csharp
options.AddDocumentTransformer(new RateLimitHeadersTransformer());
```

### Tag Filter Transformer

Include or exclude operations by tag:

```csharp
// Only include specific tags
options.AddDocumentTransformer(new TagFilterTransformer(
    includeTags: new[] { "Users", "Products" }
));

// Exclude specific tags
options.AddDocumentTransformer(new TagFilterTransformer(
    excludeTags: new[] { "Internal", "Debug" }
));
```

### Deprecation Transformer

Enhance deprecated operations with additional metadata:

```csharp
options.AddDocumentTransformer(new DeprecationTransformer(
    defaultMessage: "This endpoint will be removed on 2025-06-01",
    sunsetDate: new DateTime(2025, 6, 1)
));
```

## Middleware Configuration

### Traditional ASP.NET Core (Controllers)

```csharp
var app = builder.Build();

app.UseRouting();
app.UseMvp24HoursNativeOpenApi();
app.UseEndpoints(endpoints => { ... });
```

### Minimal APIs

```csharp
var app = builder.Build();

app.MapMvp24HoursNativeOpenApi();
app.MapGet("/api/hello", () => "Hello, World!");

app.Run();
```

## Visualization Options

### Swagger UI

Swagger UI is included when `EnableSwaggerUI = true`:

- **URL**: `/{SwaggerUIRoutePrefix}/index.html` (default: `/swagger/index.html`)
- **Standalone**: `/{SwaggerUIRoutePrefix}/standalone` (CDN-based, no middleware required)

### ReDoc

ReDoc is included when `EnableReDoc = true`:

- **URL**: `/{ReDocRoutePrefix}/index.html` (default: `/redoc/index.html`)
- **Standalone**: `/{ReDocRoutePrefix}/standalone` (CDN-based, no middleware required)

### Document Index

A JSON index of all available documents is exposed at:

```
GET /openapi
```

Response:
```json
{
  "documents": [
    { "name": "v1", "version": "1.0.0", "url": "/openapi/v1.json" },
    { "name": "v2", "version": "2.0.0", "url": "/openapi/v2.json" }
  ]
}
```

## Server Configuration

### Static Servers

```csharp
options.IncludeServerInfo = true;
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://api.example.com",
    Description = "Production server"
});
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://staging-api.example.com",
    Description = "Staging server"
});
```

### Templated Servers

```csharp
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://{environment}.api.example.com",
    Description = "Environment-specific server",
    Variables = new Dictionary<string, OpenApiServerVariable>
    {
        ["environment"] = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Environment name",
            Enum = new List<string> { "dev", "staging", "prod" }
        }
    }
});
```

## Tags Configuration

```csharp
options.Tags.Add(new OpenApiTagInfo
{
    Name = "Users",
    Description = "User management operations",
    ExternalDocsUrl = "https://docs.example.com/users"
});

options.Tags.Add(new OpenApiTagInfo
{
    Name = "Products",
    Description = "Product catalog operations"
});
```

## Migration from Swashbuckle

### Before (Swashbuckle)

```csharp
// ⚠️ DEPRECATED
services.AddMvp24HoursWebSwagger(
    title: "My API",
    version: "v1",
    enableExample: true,
    oAuthScheme: SwaggerAuthorizationScheme.Bearer
);

// In pipeline
app.UseSwagger();
app.UseSwaggerUI();
```

### After (Native OpenAPI)

```csharp
// ✅ Recommended
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});

// In pipeline
app.MapMvp24HoursNativeOpenApi();
```

## Custom Document Transformers

Create your own transformer by implementing `IOpenApiDocumentTransformer`:

```csharp
public class MyCustomTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Add custom logo
        document.Info.Extensions["x-logo"] = new OpenApiObject
        {
            ["url"] = new OpenApiString("https://example.com/logo.png"),
            ["altText"] = new OpenApiString("My API")
        };
        
        return Task.CompletedTask;
    }
}

// Register
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<MyCustomTransformer>();
});
```

## Best Practices

1. **Use Document Transformers** for consistent modifications across all operations
2. **Enable ProblemDetails** for standardized error responses
3. **Configure Authentication** at the document level, not per-operation
4. **Use Tags** to organize operations logically
5. **Set Up Versioning** early in the project lifecycle
6. **Include Server Information** for production deployments
7. **Add Rate Limit Headers** if your API has rate limiting

## Troubleshooting

### Document Not Generated

Ensure `AddEndpointsApiExplorer()` is called when using Swagger UI:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMvp24HoursNativeOpenApi(options => { ... });
```

### Swagger UI Not Loading

Check that the route prefix matches your configuration:

```csharp
options.SwaggerUIRoutePrefix = "api-docs"; // Access at /api-docs/index.html
```

### Security Scheme Not Applied

Ensure the authentication scheme is configured before building the app:

```csharp
options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
options.BearerSecurityScheme = new OpenApiBearerSecurityScheme { ... };
```

## Related Documentation

- [ProblemDetails (RFC 7807)](problem-details.md)
- [Minimal APIs with TypedResults](minimal-apis.md)
- [Source Generators](source-generators.md)

