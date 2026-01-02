# Padrões de Segurança para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões ao implementar autenticação, autorização e recursos de segurança em aplicações baseadas em Mvp24Hours.

---

## Métodos de Autenticação

| Método | Caso de Uso | Pacote |
|--------|-------------|--------|
| JWT Bearer | Autenticação de API | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| API Key | Autenticação Simples de Serviço | Middleware customizado |
| OAuth2/OIDC | Provedores de Identidade Externos | `Microsoft.AspNetCore.Authentication.OpenIdConnect` |
| Basic Auth | Sistemas Legados/Simples | `Microsoft.AspNetCore.Authentication` |

---

## Autenticação JWT

### Configuração

```csharp
// Extensions/AuthenticationExtensions.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ProjectName.WebAPI.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
```

### Serviço de Token JWT

```csharp
// Services/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProjectName.WebAPI.Services;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(UserDto user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Adicionar roles
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
```

### Configuração (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "SuaChaveSecretaSuperSeguraQueTemPeloMenos32Caracteres!",
    "Issuer": "ProjectName.API",
    "Audience": "ProjectName.Client",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## Autenticação por API Key

### Middleware de API Key

```csharp
// Middlewares/ApiKeyMiddleware.cs
namespace ProjectName.WebAPI.Middlewares;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pular autenticação para certos caminhos
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "API Key ausente" });
            return;
        }

        var validApiKeys = _configuration.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();

        if (!validApiKeys.Contains(extractedApiKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "API Key inválida" });
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyMiddleware>();
    }
}
```

### Atributo de API Key para Endpoints Específicos

```csharp
// Attributes/ApiKeyAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectName.WebAPI.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "API Key ausente" });
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var validApiKeys = configuration.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();

        if (!validApiKeys.Contains(extractedApiKey.ToString()))
        {
            context.Result = new ObjectResult(new { message = "API Key inválida" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
```

---

## Autorização Baseada em Roles

### Configuração de Autorização

```csharp
// Extensions/AuthorizationExtensions.cs
namespace ProjectName.WebAPI.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Políticas baseadas em roles
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
            
            // Políticas baseadas em claims
            options.AddPolicy("CanManageCustomers", policy => 
                policy.RequireClaim("permission", "customers:manage"));
            
            options.AddPolicy("CanViewReports", policy => 
                policy.RequireClaim("permission", "reports:view"));

            // Políticas combinadas
            options.AddPolicy("FullAccess", policy =>
            {
                policy.RequireRole("Admin");
                policy.RequireClaim("permission", "full-access");
            });

            // Requisito customizado
            options.AddPolicy("MinimumAge", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(18)));
        });

        return services;
    }
}
```

### Handler de Autorização Customizado

```csharp
// Authorization/MinimumAgeRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace ProjectName.WebAPI.Authorization;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst(c => c.Type == "DateOfBirth");

        if (dateOfBirthClaim == null)
        {
            return Task.CompletedTask;
        }

        var dateOfBirth = DateTime.Parse(dateOfBirthClaim.Value);
        var age = DateTime.Today.Year - dateOfBirth.Year;

        if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }

        if (age >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### Controller com Autorização

```csharp
// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectName.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação
public class AdminController : ControllerBase
{
    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetUsers()
    {
        return Ok(new { message = "Endpoint apenas para admin" });
    }

    [HttpGet("reports")]
    [Authorize(Policy = "CanViewReports")]
    public IActionResult GetReports()
    {
        return Ok(new { message = "Endpoint de relatórios" });
    }

    [HttpPost("customers")]
    [Authorize(Policy = "CanManageCustomers")]
    public IActionResult ManageCustomers()
    {
        return Ok(new { message = "Endpoint de gerenciamento de clientes" });
    }

    [HttpGet("public")]
    [AllowAnonymous] // Sobrescreve autorização a nível de classe
    public IActionResult PublicEndpoint()
    {
        return Ok(new { message = "Endpoint público" });
    }
}
```

---

## Hash de Senha

```csharp
// Services/PasswordHasher.cs
using System.Security.Cryptography;

namespace ProjectName.WebAPI.Services;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var newHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return CryptographicOperations.FixedTimeEquals(hash, newHash);
    }
}
```

---

## Configuração de CORS

```csharp
// Extensions/CorsExtensions.cs
namespace ProjectName.WebAPI.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("Default", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}
```

---

## Rate Limiting

```csharp
// Extensions/RateLimitingExtensions.cs
using System.Threading.RateLimiting;

namespace ProjectName.WebAPI.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Limitador de janela fixa
            options.AddFixedWindowLimiter("fixed", config =>
            {
                config.Window = TimeSpan.FromMinutes(1);
                config.PermitLimit = 100;
                config.QueueLimit = 10;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Limitador de janela deslizante
            options.AddSlidingWindowLimiter("sliding", config =>
            {
                config.Window = TimeSpan.FromMinutes(1);
                config.PermitLimit = 100;
                config.SegmentsPerWindow = 4;
            });

            // Limitador por token bucket
            options.AddTokenBucketLimiter("token", config =>
            {
                config.TokenLimit = 100;
                config.TokensPerPeriod = 10;
                config.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
            });

            // Rate limiting por usuário
            options.AddPolicy("per-user", context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 50
                    });
            });
        });

        return services;
    }
}
```

---

## Middleware de Headers de Segurança

```csharp
// Middlewares/SecurityHeadersMiddleware.cs
namespace ProjectName.WebAPI.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevenir clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        
        // Prevenir sniffing de tipo MIME
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        // Habilitar filtro XSS
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        
        // Política de referência
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Política de Segurança de Conteúdo
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");
        
        // Política de Permissões
        context.Response.Headers.Append("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

---

## Configuração de Startup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCustomAuthorization();
builder.Services.AddCustomCors(builder.Configuration);
builder.Services.AddCustomRateLimiting();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthorizationHandler, MinimumAgeHandler>();

var app = builder.Build();

// Configurar pipeline
app.UseSecurityHeaders();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Padrões de Tratamento de Erros](error-handling-patterns.md)
- [Padrões de Testes](testing-patterns.md)

