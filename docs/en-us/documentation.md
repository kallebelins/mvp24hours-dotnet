# Documentation
The habit of documenting interfaces and data classes (value objects, dtos, entities, ...) can help to facilitate code maintenance.

## Swagger
Swagger allows you to easily document your RESTful API by sharing with other developers how they can consume the available resources.

### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.0.101
```

### Settings
```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
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

/// Startup.cs
services.AddMvp24HoursSwagger(
    "Pipeline API",
    version: "v1",
    xmlCommentsFileName: "NameAPI.WebAPI.xml");

```
To present code examples, use "enableExample" in the registry and the "example" tag in the comments:
```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
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
/// Startup.cs
services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer); // SwaggerAuthorizationScheme.Basic
```

If you have a custom type to work with authorizations, simply register:
```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer, // SwaggerAuthorizationScheme.Basic
    authTypes: new Type[] { typeof(AuthorizeAttribute) });
```