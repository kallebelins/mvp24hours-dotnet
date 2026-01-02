# Matriz de Decisão para Agentes de IA

> **Instrução para Agente de IA**: Use esta matriz de decisão para selecionar o template de arquitetura e padrões apropriados com base nos requisitos do projeto.

---

## Seleção de Template de Arquitetura

### Árvore de Decisão Rápida

```
É um microsserviço simples ou protótipo rápido?
├── SIM → Use Template Minimal API
└── NÃO
    ├── Requer regras de negócio complexas?
    │   ├── SIM → Use Template Complex N-Layers
    │   └── NÃO → Use Template Simple N-Layers
    └── Precisa de integrações externas (Ports & Adapters)?
        ├── SIM → Use Template Complex Pipeline Ports & Adapters
        └── NÃO → Use template padrão baseado na complexidade
```

### Matriz de Seleção de Template

| Requisito | Minimal API | Simple N-Layers | Complex N-Layers |
|-----------|:-----------:|:---------------:|:----------------:|
| Operações CRUD simples | ✅ | ✅ | ✅ |
| Regras de negócio complexas | ❌ | ⚠️ | ✅ |
| Múltiplas entidades relacionadas | ⚠️ | ✅ | ✅ |
| Validação de dados (FluentValidation) | ✅ | ✅ | ✅ |
| Padrão Specification | ❌ | ⚠️ | ✅ |
| Abstração de camada de serviço | ❌ | ✅ | ✅ |
| Separação de DTOs | ⚠️ | ✅ | ✅ |
| AutoMapper | ⚠️ | ✅ | ✅ |
| Testes unitários | ⚠️ | ✅ | ✅ |
| Requisitos de alta segurança | ❌ | ⚠️ | ✅ |
| Múltiplos consumidores (API, Services) | ❌ | ✅ | ✅ |
| Conceitos DDD | ❌ | ⚠️ | ✅ |

**Legenda**: ✅ Recomendado | ⚠️ Possível mas limitado | ❌ Não recomendado

---

## Seleção de Banco de Dados

### Árvore de Decisão Rápida

```
Que tipo de dados você vai armazenar?
├── Dados estruturados com relacionamentos
│   ├── Alto desempenho de leitura necessário → SQL Server + Dapper (queries)
│   ├── CRUD padrão → SQL Server / PostgreSQL / MySQL + EF
│   └── Precisa de transações → SQL Server / PostgreSQL + EF + UnitOfWork
├── Baseado em documentos (schema flexível)
│   └── Use MongoDB
├── Chave-valor / Cache
│   └── Use Redis
└── Requisitos mistos
    └── Use Híbrido (EF + Dapper) ou (EF + MongoDB)
```

### Matriz de Seleção de Banco de Dados

| Requisito | SQL Server | PostgreSQL | MySQL | MongoDB | Redis |
|-----------|:----------:|:----------:|:-----:|:-------:|:-----:|
| Transações ACID | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| Queries complexas | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| Suporte JSON | ✅ | ✅ | ✅ | ✅ | ✅ |
| Busca full-text | ✅ | ✅ | ✅ | ✅ | ❌ |
| Escalabilidade horizontal | ⚠️ | ⚠️ | ⚠️ | ✅ | ✅ |
| Alto throughput de escrita | ⚠️ | ✅ | ⚠️ | ✅ | ✅ |
| Schema flexível | ❌ | ⚠️ | ❌ | ✅ | ✅ |
| Dados em tempo real | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ✅ |
| Cache/Sessão | ❌ | ❌ | ❌ | ⚠️ | ✅ |
| Relacionamentos | ✅ | ✅ | ✅ | ⚠️ | ❌ |
| Custo (cloud) | $$$ | $$ | $ | $$ | $$ |

---

## Seleção de Padrão de Mensageria

### Árvore de Decisão Rápida

```
A aplicação precisa de processamento assíncrono?
├── NÃO → Use chamadas de API síncronas
└── SIM
    ├── Precisa de entrega garantida?
    │   └── Use RabbitMQ com padrão Outbox
    ├── Processamento em background simples?
    │   └── Use HostedService
    └── Workflows complexos?
        └── Use RabbitMQ + Pipeline pattern
```

### Matriz de Seleção de Mensageria

| Requisito | API Direta | RabbitMQ | Hosted Service |
|-----------|:----------:|:--------:|:--------------:|
| Resposta síncrona | ✅ | ❌ | ❌ |
| Fire and forget | ❌ | ✅ | ✅ |
| Entrega garantida | ✅ | ✅ | ⚠️ |
| Mecanismo de retry | ⚠️ | ✅ | ⚠️ |
| Balanceamento de carga | ❌ | ✅ | ❌ |
| Múltiplos consumidores | ❌ | ✅ | ❌ |
| Processamento em background | ❌ | ✅ | ✅ |
| Tarefas de longa duração | ❌ | ✅ | ✅ |
| Atualizações em tempo real | ✅ | ⚠️ | ❌ |

---

## Seleção de Padrões

### Padrão de Validação

| Cenário | Padrão Recomendado |
|---------|-------------------|
| Validação simples de propriedade | Data Annotations |
| Validação de negócio complexa | FluentValidation |
| Validação entre campos | FluentValidation |
| Validação assíncrona | FluentValidation |
| Mensagens localizadas | FluentValidation com Resources |

### Uso do Padrão Repository

| Cenário | Implementação |
|---------|---------------|
| CRUD simples | `IRepository<T>` |
| Operações assíncronas | `IRepositoryAsync<T>` |
| Queries customizadas | `ISpecificationQuery<T>` |
| Leituras otimizadas | Dapper + Custom Repository |
| Caching | Repository + Redis |

---

## Combinações de Padrões de Arquitetura

### Combinações Recomendadas

#### 1. REST API Simples (Baixa Complexidade)

```
Template: Minimal API
Banco de Dados: PostgreSQL + EF
Validação: FluentValidation
Logging: NLog
Health Checks: Sim
```

#### 2. Aplicação de Negócio Padrão (Média Complexidade)

```
Template: Simple N-Layers
Banco de Dados: SQL Server + EF
Validação: FluentValidation
Mapeamento: AutoMapper
Logging: NLog
Health Checks: Sim
Swagger: Sim
```

#### 3. Aplicação Corporativa (Alta Complexidade)

```
Template: Complex N-Layers
Banco de Dados: SQL Server + EF + Dapper
Validação: FluentValidation
Mapeamento: AutoMapper
Padrões: Specification, UnitOfWork, Repository
Logging: NLog + OpenTelemetry
Health Checks: Sim
Swagger: Sim
```

#### 4. Aplicação Orientada a Eventos

```
Template: Complex N-Layers
Banco de Dados: PostgreSQL + EF
Mensageria: RabbitMQ
Padrões: Pipeline, Outbox
Validação: FluentValidation
Logging: NLog + OpenTelemetry
Health Checks: Sim
```

#### 5. Aplicação de Alta Performance em Leitura

```
Template: Complex N-Layers
Banco de Dados: SQL Server + EF (escritas) + Dapper (leituras)
Cache: Redis
Validação: FluentValidation
Mapeamento: AutoMapper
Logging: NLog
```

#### 6. Aplicação Baseada em Documentos

```
Template: Simple N-Layers
Banco de Dados: MongoDB
Validação: FluentValidation
Mapeamento: AutoMapper
Logging: NLog
Health Checks: Sim
```

---

## Opções de Modernização (.NET 9)

### Quando Aplicar Padrões Modernos

| Funcionalidade | Aplicar Quando |
|----------------|----------------|
| HybridCache | Cache distribuído com L1/L2 |
| Rate Limiting | Necessidade de throttling de API |
| HTTP Resilience | Chamadas de API externa |
| Minimal APIs | Endpoints simples, microsserviços |
| Output Caching | Cache de resposta necessário |
| Keyed Services | Múltiplas implementações |
| ProblemDetails | Respostas de erro padronizadas |

---

## Checklist para Agente de IA

Antes de gerar código, verifique:

- [ ] Template selecionado baseado na complexidade
- [ ] Banco de dados selecionado baseado nos requisitos de dados
- [ ] Padrão de mensageria selecionado (se async necessário)
- [ ] Padrão de validação selecionado
- [ ] Estratégia de logging definida
- [ ] Health checks identificados
- [ ] Funcionalidades de modernização consideradas
- [ ] Todos os pacotes NuGet necessários identificados

---

## Documentação Relacionada

- [Templates de Arquitetura](ai-context/architecture-templates.md)
- [Padrões de Banco de Dados](ai-context/database-patterns.md)
- [Padrões de Mensageria](ai-context/messaging-patterns.md)
- [Padrões de Observabilidade](ai-context/observability-patterns.md)
- [Padrões de Modernização](ai-context/modernization-patterns.md)

