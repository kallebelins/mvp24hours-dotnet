# <img  style="vertical-align:middle" width="42" height="42" src="/_media/icon.png" alt="Mvp24Hours" /> Mvp24Hours - NET9 (v9.1.200) 🚀

Este projeto foi desenvolvido para contribuir com a construção rápida de serviços com [.NET](https://learn.microsoft.com/pt-br/training/dotnet/). Usei a referência de soluções de mercado para construção de microserviços.

## 🎯 Características

### Dados e Persistência
* **Bancos Relacionais**: SQL Server, PostgreSQL, MySQL com EF Core (Interceptors, Multi-tenancy, Bulk Operations)
* **Bancos NoSQL**: MongoDB (Change Streams, GridFS, Geospatial) e Redis
* **Repository e Unit of Work**: Com Specification Pattern e Paginação por Cursor

### Mensageria e Eventos
* **Message Broker**: RabbitMQ Enterprise (Consumers Tipados, Request/Response, Scheduling, Sagas)
* **CQRS e Mediator**: Biblioteca completa com Commands, Queries, Notifications, Behaviors
* **Domain Events e Integration Events**: Com Outbox Pattern para confiabilidade

### Arquitetura e Padrões
* **Pipeline**: Padrão Pipe and Filters (Tipado, Fork/Join, Saga, Checkpoint/Resume)
* **Event Sourcing**: Aggregates, Event Store, Snapshots, Projections
* **Saga/Process Manager**: Com compensação e timeout

### Observabilidade e Resiliência
* **OpenTelemetry**: Tracing, Metrics, Logs com exporters OTLP, Console, Prometheus
* **Resiliência**: Resiliência nativa do .NET 9 (Microsoft.Extensions.Resilience)
* **Health Checks**: SQL, MongoDB, Redis, RabbitMQ com endpoints unificados

### .NET 9 Moderno
* **HybridCache**: Cache L1 + L2 com proteção contra stampede
* **Rate Limiting**: System.Threading.RateLimiting nativo
* **Minimal APIs**: TypedResults, MapCommand/MapQuery para CQRS
* **Source Generators**: [LoggerMessage] e [JsonSerializable] para AOT
* **OpenAPI Nativo**: Microsoft.AspNetCore.OpenAPI
* **.NET Aspire 9**: Integração com stack cloud-native

### Infraestrutura
* **Documentação**: Swagger/OpenAPI 3.1
* **Mapeamento**: AutoMapper integrado
* **Validação**: FluentValidation e Data Annotations
* **Segurança**: API Key auth, Rate limiting, Security headers
* **Background Jobs**: CronJob com retry, circuit breaker, distributed locking

## 📚 Exemplos
Você poderá estudar diversas soluções com a biblioteca Mvp24Hours. Visite os projetos de exemplo em:
<br>https://github.com/kallebelins/mvp24hours-dotnet-samples/blob/main/README.pt-br.md

## 🔮 Próximos Passos
* Implementar integração com Kafka (message broker)
* Criar modelo de projeto com Grpc sobre HTTP2 (servidor e cliente)
* Criar modelo de projeto para gateway (YARP) com service discovery
* Gravar vídeos de treinamento para a comunidade
* Implementar suporte a GraphQL

## ✅ Concluídos Recentemente (v9.1.200)
* **Biblioteca CQRS**: Implementação completa do Mediator (substituto do MediatR)
* **OpenTelemetry**: Stack completa de observabilidade (traces, metrics, logs)
* **Modernização .NET 9**: HybridCache, TimeProvider, RateLimiting, Channels
* **EF Core Avançado**: Interceptors, Multi-tenancy, Bulk operations
* **MongoDB Avançado**: Change Streams, GridFS, Geospatial
* **RabbitMQ Enterprise**: Consumers tipados, Sagas, Scheduling
* **Pipeline Avançado**: Tipado, Fork/Join, Checkpoint/Resume
* **50+ Docs Bilíngues**: Documentação PT-BR e EN-US

## Donativos
Por favor, considere fazer uma doação se você acha que esta biblioteca é útil para você ou que meu trabalho é valioso. Fico feliz se você puder me ajudar a [comprar uma xícara de café](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC). :heart:

## Comunidade
Usuários, interessados, estudantes, entusiastas, desenvolvedores, programadores [connecte no LinkedIn](https://www.linkedin.com/in/kallebelins/) para acompanhar de perto nosso crescimento!

## Patrocinadores
Seja um patrocinador escolhendo este projeto para acelerar seus produtos.

## O que há de novo?
Veja as novidades e atualizações desse projeto. [Novidades](pt-br/release)

## Você migrou seu projeto?
Acompanhe as mudanças para manter seu código funcionando corretamente. [Migração](pt-br/migration)


