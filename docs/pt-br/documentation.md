# Documentação
O hábito de documentar interfaces e classes de dados (value objects, dtos, entidades, ...) pode contribuir para facilitar a manutenção de código. 

## Swagger (Swashbuckle)

> ⚠️ **Nota:** Para projetos .NET 9+, considere usar [OpenAPI Nativo](modernization/native-openapi.md) ao invés do Swashbuckle. O OpenAPI Nativo é mais leve, compatível com AOT e oficialmente suportado pela Microsoft.

O Swagger permite você documentar facilmente sua API RESTful compartilhando com outros desenvolvedores a forma como poderão consumir os recursos disponíveis.

### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.1.x
```

### Configuração
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
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

/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Pipeline API",
    version: "v1",
    xmlCommentsFileName: "NameAPI.WebAPI.xml");

```
Para apresentar exemplos de código use "enableExample" no registro e a tag "example" nos comentários:
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
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer); // SwaggerAuthorizationScheme.Basic
```

Se você possui um tipo personalizado para trabalhar com autorizações, basta registrar:
```csharp
/// Program.cs
builder.Services.AddMvp24HoursSwagger(
    "Name API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer, // SwaggerAuthorizationScheme.Basic
    authTypes: new Type[] { typeof(AuthorizeAttribute) });
```

---

## OpenAPI Nativo (.NET 9+)

O .NET 9 introduz suporte nativo a OpenAPI via `Microsoft.AspNetCore.OpenApi`, fornecendo uma alternativa leve e compatível com AOT ao Swashbuckle.

### Vantagens sobre o Swashbuckle

| Recurso | OpenAPI Nativo | Swashbuckle |
|---------|---------------|-------------|
| Compatibilidade AOT | ✅ Suporte completo | ⚠️ Limitado |
| Tamanho do Pacote | ~50KB | ~500KB |
| Suporte Oficial | ✅ Microsoft | ❌ Terceiros |
| Performance | ✅ Otimizado | ⚠️ Usa reflection |

### Instalação

```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.WebAPI -Version 9.1.x
```

### Configuração Básica

```csharp
/// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adiciona OpenAPI nativo com configuração mínima
builder.Services.AddMvp24HoursNativeOpenApiMinimal("My API", "1.0.0");

var app = builder.Build();

// Mapeia os endpoints OpenAPI
app.MapMvp24HoursNativeOpenApi();

app.Run();
```

### Configuração Completa

```csharp
/// Program.cs
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.Description = "Uma API de exemplo usando OpenAPI nativo";
    
    // Habilita Swagger UI e ReDoc
    options.EnableSwaggerUI = true;
    options.EnableReDoc = true;
    
    // Autenticação
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
    options.BearerSecurityScheme = new OpenApiBearerSecurityScheme
    {
        Description = "Insira seu token JWT",
        BearerFormat = "JWT"
    };
});

var app = builder.Build();

app.MapMvp24HoursNativeOpenApi();
```

### Migração do Swashbuckle

```csharp
// ⚠️ Antes (Swashbuckle - deprecado)
services.AddMvp24HoursSwagger(
    "My API",
    version: "v1",
    oAuthScheme: SwaggerAuthorizationScheme.Bearer
);

// ✅ Depois (OpenAPI Nativo - recomendado)
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});
```

> 📚 Para documentação completa sobre OpenAPI Nativo, incluindo versionamento, transformadores de documento e recursos avançados, consulte [Documentação do OpenAPI Nativo](modernization/native-openapi.md).
