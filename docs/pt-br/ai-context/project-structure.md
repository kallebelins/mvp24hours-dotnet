# Estrutura de Projetos para Agentes de IA

> **Instrução para Agente de IA**: Esta é a página índice para estruturas de projeto. Escolha a estrutura apropriada com base na complexidade e requisitos do projeto.

---

## Estruturas Disponíveis

| Estrutura | Complexidade | Camadas | Melhor Para |
|-----------|--------------|---------|-------------|
| [Minimal API](structure-minimal-api.md) | Baixa | 1 | Microservices, CRUDs simples, MVPs |
| [Simple N-Layers](structure-simple-nlayers.md) | Média | 3 | Projetos médios, separação clara |
| [Complex N-Layers](structure-complex-nlayers.md) | Alta | 4 | Corporativo, lógica de negócio complexa |

---

## Guia Rápido de Seleção

### Use Minimal API quando:
- Construir microservices com responsabilidade única
- Criar APIs CRUD simples (1-5 entidades)
- Desenvolver protótipos ou MVPs
- Precisar de deploy leve e rápido
- Não houver lógica de negócio complexa

### Use Simple N-Layers quando:
- Construir aplicações de médio porte (5-15 entidades)
- Precisar de clara separação de responsabilidades
- Múltiplos desenvolvedores trabalhando no projeto
- Projeto pode crescer ao longo do tempo
- Lógica de negócio em serviços (não pipelines complexos)

### Use Complex N-Layers quando:
- Construir aplicações corporativas (15+ entidades)
- Lógica de negócio complexa requerendo camada de aplicação dedicada
- Usar padrões como CQRS, DDD ou Ports & Adapters
- Múltiplas fontes de dados (EF Core + Dapper)
- Projetos de longo prazo com múltiplas equipes

---

## Comparação de Estruturas

```
Minimal API (1 projeto):
└── NomeProjeto/
    ├── Entities/
    ├── ValueObjects/
    ├── Validators/
    ├── Data/
    └── Endpoints/

Simple N-Layers (3 projetos):
├── NomeProjeto.Core/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Validators/
├── NomeProjeto.Infrastructure/
│   └── Data/
└── NomeProjeto.WebAPI/
    └── Controllers/

Complex N-Layers (4 projetos):
├── NomeProjeto.Core/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Validators/
│   └── Contract/
├── NomeProjeto.Infrastructure/
│   └── Data/
├── NomeProjeto.Application/
│   ├── Services/
│   ├── Mappings/
│   └── Pipelines/
└── NomeProjeto.WebAPI/
    ├── Controllers/
    └── Middlewares/
```

---

## Convenções de Nomenclatura Comuns

Estas convenções se aplicam a todas as estruturas:

### Arquivos e Pastas

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Entidade | PascalCase, singular | `Cliente.cs` |
| DTO | PascalCase + sufixo Dto | `ClienteDto.cs` |
| DTO de criação | PascalCase + CreateDto | `ClienteCreateDto.cs` |
| DTO de atualização | PascalCase + UpdateDto | `ClienteUpdateDto.cs` |
| DTO de filtro | PascalCase + FiltroDto | `ClienteFiltroDto.cs` |
| Validator | PascalCase + Validator | `ClienteValidator.cs` |
| Service | PascalCase + Service | `ClienteService.cs` |
| Repository | PascalCase + Repository | `ClienteRepository.cs` |
| Controller | PascalCase + Controller | `ClienteController.cs` |
| Configuration | PascalCase + Configuration | `ClienteConfiguration.cs` |
| Specification | Descritivo + Spec | `ClientePorFiltroSpec.cs` |
| Consumer | Mensagem + Consumer | `ClienteCriadoConsumer.cs` |
| Operation | Verbo + Entidade + Operation | `ValidarClienteOperation.cs` |
| Profile (AutoMapper) | PascalCase + Profile | `ClienteProfile.cs` |

### Padrões de Namespace Comuns

```csharp
// Camada Core
{NomeProjeto}.Core.Entities
{NomeProjeto}.Core.ValueObjects
{NomeProjeto}.Core.Validators
{NomeProjeto}.Core.Contract.Services
{NomeProjeto}.Core.Specifications

// Camada Infrastructure
{NomeProjeto}.Infrastructure.Data
{NomeProjeto}.Infrastructure.Data.Configurations

// Camada Application (apenas Complex)
{NomeProjeto}.Application.Services
{NomeProjeto}.Application.Mappings
{NomeProjeto}.Application.Pipelines

// Camada WebAPI
{NomeProjeto}.WebAPI.Controllers
{NomeProjeto}.WebAPI.Extensions
{NomeProjeto}.WebAPI.Middlewares
```

---

## Arquivos de Configuração (Todas as Estruturas)

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

## Classes Base de Entidade

Todas as entidades devem herdar das classes base do Mvp24Hours:

```csharp
// Para entidades com ID inteiro auto-incremento
public class Cliente : EntityBase<int>
{
    public string Nome { get; set; } = string.Empty;
}

// Para entidades com ID GUID
public class Pedido : EntityBase<Guid>
{
    public DateTime DataPedido { get; set; }
}

// Para entidades com campos de auditoria (Criado, Modificado, etc.)
public class EntidadeAuditavel : EntityBaseLog<int, int>
{
    public string Descricao { get; set; } = string.Empty;
}
```

---

## Padrões de DTO

### DTOs Record (Recomendado)

```csharp
// DTO de Resposta
public record ClienteDto(
    int Id,
    string Nome,
    string Email,
    bool Ativo
);

// DTO de Criação
public record ClienteCreateDto(
    string Nome,
    string Email
);

// DTO de Atualização
public record ClienteUpdateDto(
    string Nome,
    string Email,
    bool Ativo
);

// DTO de Filtro (para queries)
public record ClienteFiltroDto(
    string? Nome,
    string? Email,
    bool? Ativo
);
```

### DTOs Classe (Quando mutação é necessária)

```csharp
public class ClienteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Banco de Dados](database-patterns.md)
- [Padrões de Mensageria](messaging-patterns.md)
- [Padrões de Observabilidade](observability-patterns.md)
