# Documentação
O hábito de documentar interfaces e classes de dados (value objects, dtos, entidades, ...) pode contruibuir para facilitar a manutenção de código. 

## Swagger
O Swagger permite você documentar facilmente sua API RESTful compartilhando com outros desenvolvedores a forma como poderão consumir os recursos disponíveis.

### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.0.101
```

### Configuração
```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1");
```

Para apresentar comentários basta habilitar "XML Documentation File" e gerar build.
```csharp
/// NameAPI.WebAPI.csproj
// configurar projeto para extrair comentários
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <DocumentationFile>.\NameAPI.WebAPI.xml</DocumentationFile>
</PropertyGroup>

/// Startup.cs
services.AddMvp24HoursSwagger(
    "Pipeline API",
    version: "v1",
    xmlCommentsFileName: "NameAPI.WebAPI.xml");

```
Para apresentar exemplos de código use "enableExample" no registro e a tag "example" nos comentários:
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
    /// A data da previsão em qualquer formato ISO
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Temperatura em Celsius
    /// </summary>
    /// <example>25</example>
    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// Um resumo textual
    /// </summary>
    /// <example>Nublado com possibilidade de chuva</example>
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

Para apresentar cadeado de segurança para requisições com autorização "Bearer" ou "Basic" faça:

```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer); // SwaggerAuthorizationScheme.Basic
```

Se você possui um tipo personalizado para trabalhar com autorizações, basta registrar:
```csharp
/// Startup.cs
services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer, // SwaggerAuthorizationScheme.Basic
    authTypes: new Type[] { typeof(AuthorizeAttribute) });
```