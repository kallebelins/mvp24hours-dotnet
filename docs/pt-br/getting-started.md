# Início

Cada solução arquitetural deve ser construída baseada nas necessidades técnicas e/ou de negócio.
O objetivo dessa biblioteca é garantir agilidade na construção de produtos digitais através de estruturas, mecanismos e ferramentas que, combinados corretamente, oferecem robustez, segurança, desempenho, monitoramento, observabilidade, resiliência e consistência.

## 🚀 Instalação Rápida

```bash
# Core (obrigatório)
dotnet add package Mvp24Hours.Core

# Escolha o módulo de dados
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore    # SQL Server, PostgreSQL, MySQL
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb   # MongoDB

# CQRS e Mediator (recomendado)
dotnet add package Mvp24Hours.Infrastructure.Cqrs

# WebAPI
dotnet add package Mvp24Hours.WebAPI

# Mensageria
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ

# Cache
dotnet add package Mvp24Hours.Infrastructure.Caching
```

## 📋 Guia de Funcionalidades

### 🗄️ Banco de Dados Relacional
É um banco de dados que permite criar relacionamentos entre si com o objetivo de garantir consistência e integridade dos dados.

| Database | Link |
|----------|------|
| SQL Server | [Configuração](database/relational?id=sql-server) |
| PostgreSQL | [Configuração](database/relational?id=postgresql) |
| MySQL | [Configuração](database/relational?id=mysql) |

**Funcionalidades avançadas:**
- Interceptors (Audit, SoftDelete, Concurrency, SlowQuery)
- Multi-tenancy com query filters automáticos
- Bulk Operations (Insert, Update, Delete)
- Specification Pattern integrado
- Read/Write splitting para replicas

### 🍃 Banco de Dados NoSQL

#### Orientado a Documentos
> Banco de dados projetado para armazenar e consultar dados como documentos JSON.

[MongoDB](database/nosql?id=mongodb) - Com Change Streams, GridFS, Geospatial queries

#### Orientado a Chave-Valor
Estrutura de dados do tipo mapa/dicionário, onde utilizamos uma chave como identificador.

[Redis](database/nosql?id=redis) - Cache distribuído e locks

### ⭐ CQRS e Mediator (Novo!)
Padrão Command Query Responsibility Segregation com Mediator próprio.

[CQRS](cqrs/home.md) - Documentação completa

**Inclui:**
- Commands e Queries tipados
- Pipeline Behaviors (Logging, Validation, Caching, Transaction, Retry)
- Domain Events e Integration Events
- Event Sourcing e Sagas
- Idempotência e Scheduled Commands

### 📨 Message Broker
Software que possibilita que aplicações, sistemas e serviços se comuniquem.

[RabbitMQ](broker.md) - Mensageria enterprise

**Funcionalidades:**
- Consumers tipados (`IMessageConsumer<T>`)
- Request/Response pattern
- Message Scheduling
- Batch consumers
- Sagas com state machines
- Multi-tenancy

### 📦 Pipeline
Padrão Pipe and Filters que representa um tubo com diversas operações executadas sequencialmente.

[Pipeline](pipeline.md) - Documentação completa

**Funcionalidades:**
- Pipeline tipado (`IPipeline<TInput, TOutput>`)
- Fork/Join para fluxos paralelos
- Saga Pattern com compensação
- Checkpoint/Resume para pipelines longos

### 📊 Observabilidade (Novo!)
Stack completa de observabilidade com OpenTelemetry.

[Observabilidade](observability/home.md) - Documentação completa

**Inclui:**
- Tracing distribuído com Activities
- Métricas (Counters, Histograms, Gauges)
- Logs estruturados com ILogger
- Exporters: OTLP, Console, Prometheus

### ⏰ CronJob
Agendamento de tarefas em background com expressões CRON.

[CronJob](cronjob.md) - Documentação completa

**Funcionalidades:**
- Retry com circuit breaker
- Distributed locking
- Health checks
- Métricas e OpenTelemetry

### 📝 Documentação
Documente sua API RESTful com Swagger/OpenAPI.

[Swagger](swagger.md) - Configuração

**Novo:** Suporte a OpenAPI nativo (.NET 9)

### 🔄 Mapeamento
AutoMapper para mapeamento de objetos (Entity ↔ DTO).

[AutoMapper](automapper.md) - Configuração

### ✅ Validação
Validação de dados com FluentValidation ou Data Annotations.

[Validação](validation.md) - Documentação

## 🏗️ Padrões Arquiteturais

| Padrão | Descrição | Link |
|--------|-----------|------|
| **Unit of Work** | Gerencia transações e persistência | [Documentação](database/use-unitofwork.md) |
| **Repository** | Abstração de acesso a dados | [Documentação](database/use-repository.md) |
| **Repository Service** | Regras de negócio + repositório | [Documentação](database/use-service.md) |
| **Specification** | Filtros reutilizáveis | [Documentação](specification.md) |
| **CQRS** | Separação de leitura/escrita | [Documentação](cqrs/home.md) |
| **Event Sourcing** | Persistência por eventos | [Documentação](cqrs/event-sourcing/home.md) |
| **Saga** | Transações distribuídas | [Documentação](cqrs/saga/home.md) |

## 🔧 Modernização .NET 9

Funcionalidades nativas do .NET 9 integradas:

| Funcionalidade | Descrição | Link |
|----------------|-----------|------|
| **HybridCache** | Cache L1 + L2 com stampede protection | [Documentação](modernization/hybrid-cache.md) |
| **TimeProvider** | Abstração de tempo para testes | [Documentação](modernization/time-provider.md) |
| **Rate Limiting** | Limitação de requisições nativa | [Documentação](modernization/rate-limiting.md) |
| **Channels** | Producer/Consumer de alta performance | [Documentação](modernization/channels.md) |
| **TypedResults** | Minimal APIs tipadas | [Documentação](modernization/minimal-apis.md) |

## 📚 Próximos Passos

1. **Escolha seu banco de dados** e configure seguindo a documentação
2. **Configure o CQRS** se precisar de Commands/Queries estruturados
3. **Adicione observabilidade** para monitoramento em produção
4. **Explore os exemplos** em [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
