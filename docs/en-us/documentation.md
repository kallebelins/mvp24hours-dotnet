# Documentation
The habit of documenting interfaces and data classes (value objects, dtos, entities, ...) can help to facilitate code maintenance.

## Swagger (Swashbuckle)

> ⚠️ **Note:** For .NET 9+ projects, consider using [Native OpenAPI](modernization/native-openapi.md) instead of Swashbuckle. Native OpenAPI is lighter, AOT-compatible, and officially supported by Microsoft.

Swagger allows you to easily document your RESTful API by sharing with other developers how they can consume the available resources.

### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.1.x
```

### Settings
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1");
```

To present comments, simply enable "XML Documentation File" and generate build.
```csharp
/// NameAPI.WebAPI.csproj
// configure project to extract comments
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <DocumentationFile>.\NameAPI.WebAPI.xml</DocumentationFile>
</PropertyGroup>

/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Pipeline API",
    version: "v1",
    xmlCommentsFileName: "NameAPI.WebAPI.xml");

```
To present code examples, use "enableExample" in the registry and the "example" tag in the comments:
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Pipeline API",
    version: "v1",
    enableExample: true);

/// WeatherForecast.cs -> Model
public class WeatherForecast
{
    /// <summary>
    /// The date of the forecast in ISO-whatever format
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Temperature in celcius
    /// </summary>
    /// <example>25</example>
    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// A textual summary
    /// </summary>
    /// <example>Cloudy with a chance of rain</example>
    public string Summary { get; set; }
}

/// WeatherController.cs
[HttpPost]
[Route("", Name = "WeatherPost")]
public IActionResult Post(WeatherForecast forecast)
{
    // ...
}

```

To present a security lock for requests with "Bearer" or "Basic" authorization, do:
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer); // SwaggerAuthorizationScheme.Basic
```

If you have a custom type to work with authorizations, simply register:
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer, // SwaggerAuthorizationScheme.Basic
    authTypes: new Type[] { typeof(AuthorizeAttribute) });
```

---

## Native OpenAPI (.NET 9+)

.NET 9 introduces native OpenAPI support via `Microsoft.AspNetCore.OpenApi`, providing a lightweight, AOT-compatible alternative to Swashbuckle.

### Benefits Over Swashbuckle

| Feature | Native OpenAPI | Swashbuckle |
|---------|---------------|-------------|
| AOT Compatibility | ✅ Full support | ⚠️ Limited |
| Package Size | ~50KB | ~500KB |
| First-party Support | ✅ Microsoft | ❌ Third-party |
| Performance | ✅ Optimized | ⚠️ Reflection-heavy |

### Setup

```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.1.x
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

### Migration from Swashbuckle

```csharp
// ⚠️ Before (Swashbuckle - deprecated)
services.AddMvp24HoursSwagger(
    "My API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer
);

// ✅ After (Native OpenAPI - recommended)
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});
```

> 📚 For complete documentation on Native OpenAPI, including versioning, document transformers, and advanced features, see [Native OpenAPI Documentation](modernization/native-openapi.md).
