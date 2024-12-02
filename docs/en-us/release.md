# What's new?

#NET9

## 9.0.101
* Migration and refactoring of evolution to NET9.

# NET8

## 8.3.261
* CronJob implementation.

## 8.2.102
* Implementation of route handlers for conversion and binding of parameters for Minimal API.

## 8.2.101
* Migration and refactoring of evolution to NET8.

# NETCORE

## 4.1.191
* Refactoring for asynchronous result mapping;

## 4.1.181
* Anti-pattern removal;
* Separation of log entity contexts for contract use only;
* Update and detail architectural resources in documentation;
* Correction of dependency injection in the RabbitMQ and Pipeline client;
* Configuration of isolated consumers for RabbitMQ client;
* Implementation of tests for database context with log;

## 3.12.262
* Refactoring extensions.

## 3.12.261
* Middleware test implementation.

## 3.12.221
* Implementation of Delegation Handlers to propagate keys in the Header (correlation-id, authorization, etc);
* Implementation of Polly to apply concepts of resilience and fault tolerance;
* Correction of automatic loading of mapping classes with IMapFrom;

## 3.12.151
* Removed generic typing from the IMapFrom class;
* Implementation of Testcontainers for RabbitMQ, Redis and MongoDb projects;

## 3.2.241
* Refactoring to migrate json file settings to fluent extensions;
* Replacement of the notification pattern;
* Review of templates;
* Addition of HealthCheck to all samples;
* Creation of a basic WebStatus project with HealthCheckUI;
* Replacement of logging dependencies for trace injection through actions;
* Trace/Verbose in all main libraries and layers;
* Configuration of transaction isolation level for queries with EF;
* Refactoring of the RabbitMQ library for consumer injection and fluid configuration for "DeadLetterQueue";
* Persistent connection and resilience with Polly for RabbitMQ;
* Implementation of asynchronous consumer for RabbitMQ;
* Pipeline adjustment to allow adding messages to the package (info, error, warning, success) - replacement of the notification pattern;
* Validation change (FluentValidation or DataAnnotations) to return list of messages - replacement of notification pattern;
* Changed documentation and added configuration for WebAPI;
* Refactoring of library testing;
* Refactoring for migration from Core to .NET 6.

## Other versions...
* Relational database (SQL Server, PostgreSql and MySql)
* NoSql database (MongoDb and Redis)
* Message Broker (RabbitMQ)
* Pipeline (Pipe and Filters pattern)
* Documentation (Swagger)
* Mapping (AutoMapper)
* Logging
* Standards for data validation (FluentValidation and Data Annotations), specifications (Specification pattern), work unit, repository, among others.