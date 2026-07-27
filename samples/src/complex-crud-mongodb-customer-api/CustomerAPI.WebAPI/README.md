# CustomerAPI - CRUD - MongoDb - Complex
N-tier project used to develop APIs where the business needs to apply complex rules, higher level of security, less data traffic, validation of sensitive data and separation of responsibilities or consumption by other technologies and projects.

## Features:
- NoSQL database (MongoDb); 
- Native OpenAPI;
- Mapping (AutoMapper); 
- Logging (NLog); 
- Patterns for data validation (FluentValidation and Data Annotations);
- Specification pattern;
- Unit of Work (Transaction);
- Repository (Paging, List, Create, Update, Delete) - Query apply: Filter, Paging - No navigation;
- FluentAPI configuration EF;
- Facade pattern;
- Dependency injection (IoC);
- Using ActionResult for API resources (Restful);
- Middlewares for handling unmanaged failures;
- DDD concepts;

## HTTP contract and runtime defaults
- In non-production environments, native OpenAPI JSON is available at `/openapi/v1.json`, with Swagger UI at `/swagger`.
- Expected validation and not-found outcomes keep the existing Mvp24Hours business and notification envelopes; unexpected exceptions use RFC ProblemDetails.
- This controller-based sample uses controller `ActionResult` responses and declared contracts.
- Settings are strongly typed and validated on startup.
- Logging uses `ILogger<T>` with the NLog provider.

## Layers:

### Core
Heart of the application. In this project we define the business: entities, valueobjects/dtos, validations, service contracts, enumerators, messages, specifications, builders or any other business definition.

### Infrastructure
Layer used to deal with issues related to infrastructure: database, web requests, reading/writing files, or rather, any connection to machine or network resources.

### Application
Layer where we implement/develop the rules defined in the "Core". We use this project as a gateway to the business frontier, which means that we will be able to consume business rules in different technologies (desktop, web api, web services, web mvc, web forms, hosted services, etc.).

### WebAPI
Layer that lies on the project boundary. We use this project to make the resources (data and actions) of our API available. Our client will connect via HTTP requests to get resources in JSON format ("application/json").

## NoSQL Database

### MongoDb (Document Oriented)
https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/nosql?id=mongodb