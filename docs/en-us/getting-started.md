# Getting Started
Each architectural solution must be built based on technical and/or business needs.
The objective of this library is to ensure agility in the construction of digital products through structures, mechanisms and tools that, when combined correctly, offer robustness, security, performance, monitoring, observability, resilience and consistency.
Below are the main references for RESTful API for persistence and service integration.

## Relational Database
It is a database that allows you to create relationships between them with the aim of guaranteeing data consistency and integrity.

* [SQL Server](en-us/database/relational?id=sql-server)
* [PostgreSql](en-us/database/relational?id=postgresql)
* [MySql](en-us/database/relational?id=mysql)

## NoSql Database
NoSQL is a generic term that represents a non-relational database.

### Document-Oriented
> It is a type of non-relational database designed to store and query data as JSON-type documents. [What is a document database](https://aws.amazon.com/pt/nosql/document/)

[MongoDb](en-us/database/nosql?id=mongodb)

### Key-value Oriented
It is a map or dictionary type data structure, where we use a key as a record identifier.

[Redis](en-us/database/nosql?id=redis)

## Message Broker
A message broker is software that enables applications, systems and services to communicate and exchange information.

[RabbitMQ](en-us/broker.md)

## Pipeline
It is a design pattern that represents a pipe with several operations (filters), executed sequentially, with the aim of traveling, integrating and/or handling a packet/message.

[Pipeline](en-us/pipeline.md)

## Documentation
The habit of documenting interfaces and data classes (value objects, dtos, entities, ...) can help to facilitate code maintenance. Swagger allows you to easily document your RESTful API by sharing with other developers how they can consume the available resources.

[Swagger](en-us/swagger.md)

## Mapping
With the practice of RESTful API development with a focus on mobile, our reference is to offer as little data as possible or necessary in each API resource. Therefore, there is a need to create specific traffic objects (DTOs).
AutoMapper helps in assigning property values of related objects (Person => PessoaDto), or better yet, mapping one object to another.

[AutoMapper](en-us/automapper.md)

## Patterns
Patterns are models that we use as a reference or basis for solving a problem. In addition to the patterns that we will present in the architectural definitions explored by this library, we explore the following references:

* [Unit of Work](en-us/database/use-unitofwork.md)
* [Repository](en-us/database/use-repository.md)
* [Repository Service](en-us/database/use-service.md): We use it to apply business rules and encapsulate the repository
* [Data Validation](en-us/validation.md): Data validation using fluent or annotations
* [Specification](en-us/specification.md): Data filter