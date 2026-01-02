# Contexto de IA - Instruções de Arquitetura

> **Objetivo**: Esta documentação foi projetada exclusivamente para agentes de IA gerarem arquiteturas .NET completas e prontas para produção usando o framework Mvp24Hours.

---

## Visão Geral

Esta seção fornece instruções estruturadas e templates para agentes de IA criarem aplicações .NET seguindo as melhores práticas e padrões implementados no ecossistema Mvp24Hours.

### Como os Agentes de IA Devem Usar Esta Documentação

1. **Analisar Requisitos**: Compreender os requisitos do projeto (tipo de banco de dados, complexidade, necessidades de mensageria)
2. **Selecionar Template**: Usar a [Matriz de Decisão](ai-context/decision-matrix.md) para escolher o template de arquitetura apropriado
3. **Aplicar Padrões**: Seguir os padrões específicos para cada componente (banco de dados, mensageria, observabilidade)
4. **Gerar Estrutura**: Criar a estrutura do projeto seguindo as convenções definidas em [Estrutura de Projetos](ai-context/project-structure.md)

---

## Templates de Arquitetura Disponíveis

| Template | Complexidade | Caso de Uso |
|----------|-------------|-------------|
| **Minimal API** | Baixa | CRUD simples, microsserviços, prototipagem rápida |
| **Simple N-Layers** | Média | Aplicações pequenas a médias com regras de negócio básicas |
| **Complex N-Layers** | Alta | Aplicações corporativas com lógica de negócio complexa |

### Categorias de Templates

#### Por Banco de Dados
- **Relacional (EF)**: SQL Server, PostgreSQL, MySQL
- **NoSQL (MongoDB)**: Armazenamento baseado em documentos
- **Chave-Valor (Redis)**: Cache e gerenciamento de sessão
- **Híbrido (EF + Dapper)**: Queries otimizadas com EF para mutações

#### Por Padrão de Arquitetura
- **CRUD**: Operações padrão de Criar, Ler, Atualizar, Excluir
- **Pipeline**: Padrão Pipe and Filters para workflows complexos
- **Ports & Adapters**: Arquitetura hexagonal para alto desacoplamento

#### Por Mensageria
- **Síncrona**: Chamadas diretas de API
- **Assíncrona (RabbitMQ)**: Integração com message broker usando Hosted Services

---

## Referência Rápida para Agentes de IA

### Pacotes Necessários (NuGet)

```text
# Framework Core
Mvp24Hours.Core
Mvp24Hours.Application

# Banco de Dados - Entity Framework
Mvp24Hours.Infrastructure.Data.EFCore
Microsoft.EntityFrameworkCore.SqlServer | Npgsql.EntityFrameworkCore.PostgreSQL | MySql.EntityFrameworkCore

# Banco de Dados - MongoDB
Mvp24Hours.Infrastructure.Data.MongoDb
MongoDB.Driver

# Banco de Dados - Redis
Mvp24Hours.Infrastructure.Caching.Redis
StackExchange.Redis

# Mensageria - RabbitMQ
Mvp24Hours.Infrastructure.RabbitMQ
RabbitMQ.Client

# Pipeline
Mvp24Hours.Infrastructure.Pipe

# Web API
Mvp24Hours.WebAPI
Swashbuckle.AspNetCore

# Mapeamento
AutoMapper
AutoMapper.Extensions.Microsoft.DependencyInjection

# Validação
FluentValidation
FluentValidation.AspNetCore

# Logging
NLog.Web.AspNetCore

# Health Checks
AspNetCore.HealthChecks.UI.Client
```

### Referências Padrão de Projeto

```xml
<!-- Projeto Core -->
<ItemGroup>
  <PackageReference Include="Mvp24Hours.Core" Version="8.*" />
  <PackageReference Include="FluentValidation" Version="11.*" />
</ItemGroup>

<!-- Projeto Infrastructure -->
<ItemGroup>
  <ProjectReference Include="..\NomeProjeto.Core\NomeProjeto.Core.csproj" />
  <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="8.*" />
</ItemGroup>

<!-- Projeto Application -->
<ItemGroup>
  <ProjectReference Include="..\NomeProjeto.Core\NomeProjeto.Core.csproj" />
  <ProjectReference Include="..\NomeProjeto.Infrastructure\NomeProjeto.Infrastructure.csproj" />
  <PackageReference Include="Mvp24Hours.Application" Version="8.*" />
  <PackageReference Include="AutoMapper" Version="12.*" />
</ItemGroup>

<!-- Projeto WebAPI -->
<ItemGroup>
  <ProjectReference Include="..\NomeProjeto.Application\NomeProjeto.Application.csproj" />
  <PackageReference Include="Mvp24Hours.WebAPI" Version="8.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
  <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
</ItemGroup>
```

---

## Instruções para Agentes de IA

### Ao Gerar Código, Sempre:

1. **Seguir Convenções do Mvp24Hours**
   - Usar `IEntityBase<TKey>` para entidades com ID tipado
   - Usar `IEntityLog` para campos de auditoria (Created, Modified, Removed)
   - Usar `IValidator<T>` do FluentValidation para validação de negócio
   - Usar `IPipelineAsync` para operações de pipeline

2. **Aplicar Injeção de Dependência**
   - Registrar serviços em `ServiceBuilderExtensions`
   - Usar métodos de extensão `IServiceCollection`
   - Seguir o padrão: `services.Add{NomeServico}()`

3. **Implementar Padrão Repository**
   - Usar `IRepository<TEntity>` para CRUD padrão
   - Usar `IRepositoryAsync<TEntity>` para operações assíncronas
   - Aplicar `ISpecificationQuery<TEntity>` para especificações de query

4. **Configurar Unit of Work**
   - Usar `IUnitOfWork` para gerenciamento de transações
   - Chamar `SaveChangesAsync()` para persistir alterações
   - Usar `IUnitOfWorkAsync` para transações assíncronas

5. **Tratar Respostas**
   - Usar `IBusinessResult<T>` para respostas de serviço
   - Aplicar `MessageResult` para mensagens de validação
   - Usar códigos de status HTTP padrão nos controllers

### Checklist de Geração de Código

- [ ] Arquivo de solução (.sln) com referências de projeto corretas
- [ ] Camada Core com entidades, DTOs, validadores, contratos
- [ ] Camada Infrastructure com DbContext, repositórios, configurações
- [ ] Camada Application com serviços, mapeamentos, facade
- [ ] Camada WebAPI com controllers, Program.cs, Startup.cs
- [ ] appsettings.json com connection strings e configuração
- [ ] Configuração de health checks
- [ ] Documentação Swagger/OpenAPI
- [ ] Configuração NLog
- [ ] Suporte Docker (opcional)

---

## Documentação Relacionada

- [Templates de Arquitetura](ai-context/architecture-templates.md)
- [Matriz de Decisão](ai-context/decision-matrix.md)
- [Padrões de Banco de Dados](ai-context/database-patterns.md)
- [Padrões de Mensageria](ai-context/messaging-patterns.md)
- [Padrões de Observabilidade](ai-context/observability-patterns.md)
- [Padrões de Modernização](ai-context/modernization-patterns.md)
- [Estrutura de Projetos](ai-context/project-structure.md)

---

## Repositório de Projetos de Exemplo

Todos os templates são baseados em implementações reais disponíveis em:
- **Repositório**: [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- **Framework**: [mvp24hours-dotnet](https://github.com/kallebelins/mvp24hours-dotnet)

