# API Versioning Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns when implementing API versioning in Mvp24Hours-based applications. Choose the appropriate versioning strategy based on the project requirements.

---

## Versioning Strategies

| Strategy | Example | Use Case |
|----------|---------|----------|
| URL Path | `/api/v1/customers` | RESTful APIs, clear separation |
| Query String | `/api/customers?api-version=1.0` | Backward compatibility |
| Header | `X-API-Version: 1.0` | Clean URLs |
| Media Type | `Accept: application/vnd.api.v1+json` | Content negotiation |

---

## Package Installation

```xml
<ItemGroup>
  <PackageReference Include="Asp.Versioning.Mvc" Version="8.*" />
  <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.*" />
</ItemGroup>
```

---

## URL Path Versioning (Recommended)

### Configuration

```csharp
// Extensions/ApiVersioningExtensions.cs
using Asp.Versioning;

namespace ProjectName.WebAPI.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
```

### Controller Implementation

```csharp
// Controllers/V1/CustomersController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ProjectName.WebAPI.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return this.ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return this.ToCreatedResult(result, nameof(GetById), new { id = result.Data?.Id });
    }
}

// Controllers/V2/CustomersController.cs
namespace ProjectName.WebAPI.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerServiceV2 _service;

    public CustomersController(ICustomerServiceV2 service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // V2 returns enhanced response with metadata
        var result = await _service.GetByIdWithMetadataAsync(id);
        return this.ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CustomerFilterDto filter)
    {
        // V2 supports advanced filtering
        var result = await _service.GetAllAsync(filter);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDtoV2 dto)
    {
        // V2 accepts additional fields
        var result = await _service.CreateAsync(dto);
        return this.ToCreatedResult(result, nameof(GetById), new { id = result.Data?.Id });
    }
}
```

---

## Query String Versioning

### Configuration

```csharp
// Extensions/ApiVersioningExtensions.cs
public static IServiceCollection AddQueryStringVersioning(this IServiceCollection services)
{
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
    });

    return services;
}
```

### Controller Implementation

```csharp
// Controllers/CustomersController.cs
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    [HttpGet("{id}")]
    [ApiVersion("1.0")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetByIdV1(int id)
    {
        // V1 implementation
    }

    [HttpGet("{id}")]
    [ApiVersion("2.0")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetByIdV2(int id)
    {
        // V2 implementation with enhanced response
    }
}
```

---

## Header Versioning

### Configuration

```csharp
// Extensions/ApiVersioningExtensions.cs
public static IServiceCollection AddHeaderVersioning(this IServiceCollection services)
{
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version");
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
    });

    return services;
}
```

---

## Combined Versioning (Multiple Readers)

```csharp
// Extensions/ApiVersioningExtensions.cs
public static IServiceCollection AddCombinedVersioning(this IServiceCollection services)
{
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        
        // Allow version to be specified in multiple ways
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("X-API-Version")
        );
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    return services;
}
```

---

## Swagger Configuration for Versioned APIs

```csharp
// Extensions/SwaggerExtensions.cs
using Microsoft.Extensions.Options;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProjectName.WebAPI.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddVersionedSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    public static IApplicationBuilder UseVersionedSwagger(this IApplicationBuilder app)
    {
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"API {description.GroupName.ToUpperInvariant()}");
            }
        });

        return app;
    }
}

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "ProjectName API",
            Version = description.ApiVersion.ToString(),
            Description = "API documentation for ProjectName",
            Contact = new OpenApiContact
            {
                Name = "Support",
                Email = "support@projectname.com"
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " (This API version has been deprecated)";
        }

        return info;
    }
}

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse 
                ? "default" 
                : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            parameter.Description ??= description?.ModelMetadata?.Description;

            if (description?.DefaultValue != null)
            {
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(
                    description.DefaultValue.ToString());
            }

            parameter.Required |= description?.IsRequired ?? false;
        }
    }
}
```

---

## Version Deprecation

### Marking Versions as Deprecated

```csharp
// Controllers/V1/CustomersController.cs
[ApiController]
[ApiVersion("1.0", Deprecated = true)]  // Mark as deprecated
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    // V1 endpoints (deprecated)
}
```

### Sunset Header

```csharp
// Middlewares/ApiVersionSunsetMiddleware.cs
namespace ProjectName.WebAPI.Middlewares;

public class ApiVersionSunsetMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, DateTime> _sunsetDates = new()
    {
        { "1.0", new DateTime(2025, 12, 31) },
        { "1.1", new DateTime(2026, 6, 30) }
    };

    public ApiVersionSunsetMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var apiVersion = context.GetRequestedApiVersion()?.ToString();
        
        if (apiVersion != null && _sunsetDates.TryGetValue(apiVersion, out var sunsetDate))
        {
            context.Response.Headers.Append("Sunset", sunsetDate.ToString("R"));
            context.Response.Headers.Append("Deprecation", "true");
            context.Response.Headers.Append("Link", 
                $"</api/v2>; rel=\"successor-version\"");
        }
    }
}
```

---

## Minimal API Versioning

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomApiVersioning();

var app = builder.Build();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// V1 endpoints
var v1 = app.MapGroup("/api/v{version:apiVersion}/customers")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(1.0);

v1.MapGet("/{id}", async (int id, ICustomerService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.HasData ? Results.Ok(result.Data) : Results.NotFound();
});

// V2 endpoints
var v2 = app.MapGroup("/api/v{version:apiVersion}/customers")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(2.0);

v2.MapGet("/{id}", async (int id, ICustomerServiceV2 service) =>
{
    var result = await service.GetByIdWithMetadataAsync(id);
    return result.HasData ? Results.Ok(result.Data) : Results.NotFound();
});

app.Run();
```

---

## DTOs per Version

### Version-Specific DTOs

```csharp
// ValueObjects/V1/CustomerDto.cs
namespace ProjectName.Core.ValueObjects.V1;

public record CustomerDto(
    int Id,
    string Name,
    string Email
);

public record CustomerCreateDto(
    string Name,
    string Email
);

// ValueObjects/V2/CustomerDto.cs
namespace ProjectName.Core.ValueObjects.V2;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    CustomerMetadata Metadata
);

public record CustomerCreateDto(
    string Name,
    string Email,
    string? Phone,
    AddressDto? Address,
    Dictionary<string, string>? Tags
);

public record CustomerMetadata(
    int OrderCount,
    decimal TotalSpent,
    string? PreferredCurrency
);

public record AddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);
```

### Mapping Between Versions

```csharp
// Mappers/CustomerVersionMapper.cs
namespace ProjectName.Application.Mappers;

public static class CustomerVersionMapper
{
    public static V2.CustomerDto ToV2(this V1.CustomerDto v1, CustomerMetadata metadata)
    {
        return new V2.CustomerDto(
            v1.Id,
            v1.Name,
            v1.Email,
            DateTimeOffset.UtcNow, // Default for upgrade
            null,
            metadata
        );
    }

    public static V1.CustomerDto ToV1(this V2.CustomerDto v2)
    {
        return new V1.CustomerDto(
            v2.Id,
            v2.Name,
            v2.Email
        );
    }
}
```

---

## Startup Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomApiVersioning();
builder.Services.AddVersionedSwagger();
builder.Services.AddControllers();

var app = builder.Build();

app.UseVersionedSwagger();
app.UseMiddleware<ApiVersionSunsetMiddleware>();
app.MapControllers();

app.Run();
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Error Handling Patterns](error-handling-patterns.md)
- [Security Patterns](security-patterns.md)

