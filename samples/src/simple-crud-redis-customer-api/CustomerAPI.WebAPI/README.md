# CustomerAPI - CRUD - Redis - Simple
N-layer project used to develop APIs where the business needs to apply simple rules.

## Features:
- NoSQL database (Redis); 
- Native OpenAPI;
- Logging (NLog); 
- Patterns for data validation (FluentValidation and Data Annotations);
- Repository (Get, Create, Delete);
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

### WebAPI
Layer that lies on the project boundary. We use this project to make the resources (data and actions) of our API available. Our client will connect via HTTP requests to get resources in JSON format ("application/json").

## NoSQL Database

### Redis (Key-Value Oriented)
https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/nosql?id=redis