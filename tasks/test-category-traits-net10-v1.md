# Evidências — Task 9.4 (Trait/Category Unit vs Integration)

Data: 16/07/2026

## Implementação

- Adicionado `[Trait("Category", "Unit")]` nas classes de teste dos projetos não-Testcontainers.
- Adicionado `[Trait("Category", "Integration")]` nas classes de teste dos projetos:
  - `Mvp24Hours.Application.Integration.Test`
  - `Mvp24Hours.Application.MongoDb.Test`
  - `Mvp24Hours.Application.Redis.Test`
  - `Mvp24Hours.Application.RabbitMQ.Test`

## Validação

### Filtro de integração

Comando:

`dotnet test src/Mvp24Hours.sln -c Debug --filter "Category=Integration"`

Resultado:

- `Mvp24Hours.Application.Integration.Test`: 69/69
- `Mvp24Hours.Application.Redis.Test`: 24/24
- `Mvp24Hours.Application.MongoDb.Test`: 11/11
- `Mvp24Hours.Application.RabbitMQ.Test`: 6/6

Total integração: **110 aprovados, 0 falhas, 0 ignorados**.

### Filtro unitário

Comando:

`dotnet test src/Mvp24Hours.sln -c Debug --filter "Category=Unit"`

Resultado:

- Filtro aplicado corretamente (projetos de integração retornam "Nenhum teste corresponde").
- Execução com **6 falhas preexistentes** em `Mvp24Hours.Core.Test.Extensions.ConvertExtensionsTest` (cenários de diacríticos/encoding), fora do escopo da task 9.4.
