# OpenAPI Nativo (.NET 9)

## Visão Geral

O .NET 9 introduz suporte nativo ao OpenAPI via `Microsoft.AspNetCore.OpenApi`, fornecendo uma alternativa leve e compatível com AOT ao Swashbuckle para documentação de APIs. O Mvp24Hours integra esta funcionalidade nativa com métodos de extensão convenientes e opções de configuração.

## Benefícios em Relação ao Swashbuckle

| Recurso | OpenAPI Nativo | Swashbuckle |
|---------|---------------|-------------|
| Compatibilidade AOT | ✅ Suporte total | ⚠️ Limitado |
| Tamanho do Pacote | ~50KB | ~500KB |
| Suporte Oficial | ✅ Microsoft | ❌ Terceiros |
| Performance | ✅ Otimizado | ⚠️ Usa reflection |
| Transformers de Documento | ✅ Nativo | ⚠️ Filtros/Convenções |

## Instalação

O OpenAPI nativo está incluído no pacote Mvp24Hours.WebAPI. O pacote `Microsoft.AspNetCore.OpenApi` é referenciado automaticamente.

```xml
<PackageReference Include="Mvp24Hours.WebAPI" Version="9.x.x" />
```

## Configuração Básica

### Configuração Mínima

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adiciona OpenAPI nativo com configuração mínima
builder.Services.AddMvp24HoursNativeOpenApiMinimal("Minha API", "1.0.0");

var app = builder.Build();

// Mapeia os endpoints OpenAPI
app.MapMvp24HoursNativeOpenApi();

app.Run();
```

### Configuração Completa

```csharp
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.DocumentName = "v1";
    options.Title = "Minha API";
    options.Version = "1.0.0";
    options.Description = "Uma API de exemplo usando OpenAPI nativo";
    
    // Informações de contato
    options.Contact = new OpenApiContactInfo
    {
        Name = "Suporte da API",
        Email = "suporte@exemplo.com.br",
        Url = "https://exemplo.com.br/suporte"
    };
    
    // Licença
    options.License = new OpenApiLicenseInfo
    {
        Name = "MIT",
        Url = "https://opensource.org/licenses/MIT",
        Identifier = "MIT"
    };
    
    // Termos de serviço
    options.TermsOfServiceUrl = "https://exemplo.com.br/termos";
    
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
```

## Suporte a Versionamento de API

### Múltiplas Versões de API

```csharp
builder.Services.AddMvp24HoursNativeOpenApiWithVersions(options =>
{
    options.Title = "Minha API";
    options.DocumentName = "v1";
    options.Version = "1.0.0";
    
    // Adiciona versões adicionais
    options.AdditionalVersions.Add(new OpenApiVersionConfig
    {
        DocumentName = "v2",
        Version = "2.0.0",
        Title = "Minha API v2",
        Description = "Versão 2 com novos recursos"
    });
    
    options.AdditionalVersions.Add(new OpenApiVersionConfig
    {
        DocumentName = "v1-deprecated",
        Version = "1.0.0",
        Title = "Minha API v1 (Descontinuada)",
        IsDeprecated = true,
        DeprecationMessage = "Por favor, migre para a v2"
    });
});
```

## Transformers de Documento

O OpenAPI nativo usa transformers de documento para customização. O Mvp24Hours fornece vários transformers integrados.

### Transformer de Esquema de Segurança

Adicionado automaticamente com base na configuração `AuthenticationScheme`:

```csharp
options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
// ou
options.AuthenticationScheme = OpenApiAuthenticationScheme.ApiKey;
options.ApiKeySecurityScheme = new OpenApiApiKeySecurityScheme
{
    Name = "X-API-Key",
    Location = ApiKeyLocation.Header,
    Description = "Autenticação por API Key"
};
```

### Transformer de Headers Customizados

Adiciona headers comuns a todas as operações:

```csharp
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer(new CustomHeadersTransformer(
        ("X-Correlation-Id", "ID de correlação para rastreamento", false),
        ("X-Tenant-Id", "Identificador do tenant", true)
    ));
});
```

### Transformer de Respostas Comuns

Adiciona respostas de erro padrão a todas as operações:

```csharp
options.AddDocumentTransformer(new CommonResponsesTransformer(
    add401: true,
    add403: true,
    add500: true,
    add503: true
));
```

### Transformer de ProblemDetails

Adiciona o schema RFC 7807 ProblemDetails às respostas de erro:

```csharp
options.AddDocumentTransformer(new ProblemDetailsTransformer());
```

### Transformer de Headers de Rate Limit

Adiciona headers de rate limit a todas as respostas:

```csharp
options.AddDocumentTransformer(new RateLimitHeadersTransformer());
```

### Transformer de Filtro por Tag

Inclui ou exclui operações por tag:

```csharp
// Inclui apenas tags específicas
options.AddDocumentTransformer(new TagFilterTransformer(
    includeTags: new[] { "Usuarios", "Produtos" }
));

// Exclui tags específicas
options.AddDocumentTransformer(new TagFilterTransformer(
    excludeTags: new[] { "Interno", "Debug" }
));
```

### Transformer de Descontinuação

Enriquece operações descontinuadas com metadados adicionais:

```csharp
options.AddDocumentTransformer(new DeprecationTransformer(
    defaultMessage: "Este endpoint será removido em 01/06/2025",
    sunsetDate: new DateTime(2025, 6, 1)
));
```

## Configuração de Middleware

### ASP.NET Core Tradicional (Controllers)

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
app.MapGet("/api/hello", () => "Olá, Mundo!");

app.Run();
```

## Opções de Visualização

### Swagger UI

O Swagger UI é incluído quando `EnableSwaggerUI = true`:

- **URL**: `/{SwaggerUIRoutePrefix}/index.html` (padrão: `/swagger/index.html`)
- **Standalone**: `/{SwaggerUIRoutePrefix}/standalone` (baseado em CDN, não requer middleware)

### ReDoc

O ReDoc é incluído quando `EnableReDoc = true`:

- **URL**: `/{ReDocRoutePrefix}/index.html` (padrão: `/redoc/index.html`)
- **Standalone**: `/{ReDocRoutePrefix}/standalone` (baseado em CDN, não requer middleware)

### Índice de Documentos

Um índice JSON de todos os documentos disponíveis é exposto em:

```
GET /openapi
```

Resposta:
```json
{
  "documents": [
    { "name": "v1", "version": "1.0.0", "url": "/openapi/v1.json" },
    { "name": "v2", "version": "2.0.0", "url": "/openapi/v2.json" }
  ]
}
```

## Configuração de Servidores

### Servidores Estáticos

```csharp
options.IncludeServerInfo = true;
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://api.exemplo.com.br",
    Description = "Servidor de produção"
});
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://staging-api.exemplo.com.br",
    Description = "Servidor de homologação"
});
```

### Servidores com Templates

```csharp
options.Servers.Add(new OpenApiServerInfo
{
    Url = "https://{ambiente}.api.exemplo.com.br",
    Description = "Servidor específico por ambiente",
    Variables = new Dictionary<string, OpenApiServerVariable>
    {
        ["ambiente"] = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Nome do ambiente",
            Enum = new List<string> { "dev", "staging", "prod" }
        }
    }
});
```

## Configuração de Tags

```csharp
options.Tags.Add(new OpenApiTagInfo
{
    Name = "Usuarios",
    Description = "Operações de gerenciamento de usuários",
    ExternalDocsUrl = "https://docs.exemplo.com.br/usuarios"
});

options.Tags.Add(new OpenApiTagInfo
{
    Name = "Produtos",
    Description = "Operações do catálogo de produtos"
});
```

## Migração do Swashbuckle

### Antes (Swashbuckle)

```csharp
// ⚠️ DESCONTINUADO
services.AddMvp24HoursWebSwagger(
    title: "Minha API",
    version: "v1",
    enableExample: true,
    oAuthScheme: SwaggerAuthorizationScheme.Bearer
);

// No pipeline
app.UseSwagger();
app.UseSwaggerUI();
```

### Depois (OpenAPI Nativo)

```csharp
// ✅ Recomendado
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Minha API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});

// No pipeline
app.MapMvp24HoursNativeOpenApi();
```

## Transformers de Documento Customizados

Crie seu próprio transformer implementando `IOpenApiDocumentTransformer`:

```csharp
public class MeuTransformerCustomizado : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Adiciona logo customizado
        document.Info.Extensions["x-logo"] = new OpenApiObject
        {
            ["url"] = new OpenApiString("https://exemplo.com.br/logo.png"),
            ["altText"] = new OpenApiString("Minha API")
        };
        
        return Task.CompletedTask;
    }
}

// Registra
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<MeuTransformerCustomizado>();
});
```

## Boas Práticas

1. **Use Transformers de Documento** para modificações consistentes em todas as operações
2. **Habilite ProblemDetails** para respostas de erro padronizadas
3. **Configure Autenticação** no nível do documento, não por operação
4. **Use Tags** para organizar operações logicamente
5. **Configure Versionamento** no início do ciclo de vida do projeto
6. **Inclua Informações de Servidor** para deployments em produção
7. **Adicione Headers de Rate Limit** se sua API tem limitação de taxa

## Solução de Problemas

### Documento Não Gerado

Certifique-se de que `AddEndpointsApiExplorer()` é chamado ao usar Swagger UI:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMvp24HoursNativeOpenApi(options => { ... });
```

### Swagger UI Não Carrega

Verifique se o prefixo da rota corresponde à sua configuração:

```csharp
options.SwaggerUIRoutePrefix = "api-docs"; // Acesso em /api-docs/index.html
```

### Esquema de Segurança Não Aplicado

Certifique-se de que o esquema de autenticação está configurado antes de construir o app:

```csharp
options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
options.BearerSecurityScheme = new OpenApiBearerSecurityScheme { ... };
```

## Documentação Relacionada

- [ProblemDetails (RFC 7807)](problem-details.md)
- [Minimal APIs com TypedResults](minimal-apis.md)
- [Source Generators](source-generators.md)

