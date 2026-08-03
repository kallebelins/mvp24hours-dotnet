# Testing Helpers and Cookbook

Mvp24Hours ships test doubles, fixtures, listeners, assertions, and data helpers in the same packages as the corresponding runtime features. There is no separate `Mvp24Hours.*.Testing` NuGet package.

## Install

Install only the packages needed by the test project:

| Area | Package | What it provides |
|---|---|---|
| Core infrastructure | `Mvp24Hours.Infrastructure` | Clock, email/SMS/file fakes, HTTP handler, logging, observability listeners, fixtures, and assertions |
| EF Core | `Mvp24Hours.Infrastructure.Data.EFCore` | EF Core InMemory registration, context factories, seeders, fake repositories, and fake units of work |
| MongoDB | `Mvp24Hours.Infrastructure.Data.MongoDb` | In-process collections, Mongo fake repositories/units of work, context factories, seeders, and container-related utilities |
| RabbitMQ | `Mvp24Hours.Infrastructure.RabbitMQ` | In-memory bus, test consume contexts, consumer/request harnesses, and message assertions |

```bash
dotnet add package Mvp24Hours.Infrastructure
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
```

Add `xunit` and a test SDK to run the examples. Add `Testcontainers.MongoDb` only when the test itself starts a MongoDB container. A real RabbitMQ container likewise requires a container library such as `Testcontainers.RabbitMq`; the Mvp24Hours RabbitMQ testing helpers described below are in-memory and do not start Docker.

Common third-party packages used alongside the helpers:

| Package | Role |
|---|---|
| `NSubstitute` or `Moq` | Mock application ports that are not covered by Mvp24Hours fakes |
| `FluentAssertions` | Readable assertions for domain and service results |
| `Bogus` | Synthetic entity/DTO builders for unit and integration data |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<TEntryPoint>` host tests |

### Suggested test project layout

```text
tests/
├── Product.UnitTests/
│   ├── Domain/
│   ├── Application/
│   └── Builders/
├── Product.IntegrationTests/
│   ├── Api/
│   ├── Persistence/
│   └── Messaging/
└── Product.TestSupport/
    └── CustomWebApplicationFactory.cs
```

Keep builders and shared factories in a support project only when more than one test project needs them.

### Naming conventions

| Kind | Pattern | Example |
|---|---|---|
| Method under test | `Method_Scenario_Expected` | `Create_WhenEmailExists_ReturnsConflict` |
| Unit fixture | `{Type}Tests` | `OrderTests` |
| Integration fixture | `{Area}IntegrationTests` | `OrdersApiIntegrationTests` |

## Choose the test boundary

Use a unit test when the behavior can be checked with `MockClock`, a fake service, fake repository, in-process Mongo collection, or `InMemoryBus`. Use an integration test when provider behavior matters: SQL translation and constraints, MongoDB transactions/indexes, a real broker, or the complete ASP.NET Core host.

The repository labels xUnit tests with:

```csharp
[Trait("Category", "Unit")]
public class PriceCalculatorTest
{
}

[Trait("Category", "Integration")]
public class CheckoutApiTest
{
}
```

Run one category with:

```bash
dotnet test src/Mvp24Hours.slnx --filter "Category=Unit"
dotnet test src/Mvp24Hours.slnx --filter "Category=Integration"
```

Main CI splits the same way on ubuntu: unit tests (`Category!=Integration`) and integration tests (`Category=Integration`, Docker required). Coverage from both jobs is merged before the regression gate. See [coverage baseline](coverage-baseline.md) for phase targets.

Local mirror (unit + integration + 45% gate):

```powershell
./scripts/run-ci-local.ps1 -SkipSamples
./scripts/run-ci-local.ps1 -SkipSamples -SkipIntegration   # no Docker
```

`Category=Integration` may require Docker or another external service. The trait describes the boundary; it is not automatically added by any Mvp24Hours helper.

## Core infrastructure registration

Import:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Testing.Extensions;
```

### Registration methods

| Method | Registrations and behavior |
|---|---|
| `AddMvpTestingInfrastructure()` | Adds `MockClock`, fake email, fake SMS, fake file storage, and `TestHttpMessageHandler` |
| `AddMockClock(initialTime?)` | Singleton `IClock` and `MockClock` |
| `AddFakeEmailService(configure?)` | Singleton `IEmailService`, `IFakeEmailService`, and `FakeEmailService` |
| `AddFakeSmsService(configure?)` | Singleton `ISmsService`, `IFakeSmsService`, and `FakeSmsService` |
| `AddFakeFileStorage(configure?)` | Singleton `IFileStorage`, `IFakeFileStorage`, and `FakeFileStorage` |
| `AddTestHttpHandler(configure?)` | Singleton `TestHttpMessageHandler`/`HttpMessageHandler` and transient `HttpClient` |
| `AddTestHttpClient(name, configureClient?, configureHandler?)` | Named client backed by the singleton test handler |
| `AddInMemoryLoggerProvider(configure?)` | Replaces logging providers, sets `Trace` minimum, and registers `InMemoryLoggerProvider` |
| `AddFakeActivityListener(sourceFilter?)` | Singleton activity listener |
| `AddFakeMeterListener(meterFilter?)` | Singleton metric listener |
| `AddObservabilityTesting(sourceFilter?)` | In-memory logging plus activity and metric listeners |
| `ReplaceWithTestInfrastructure(configure?)` | Removes selected runtime interfaces and installs configured test replacements |

`AddMvpTestingInfrastructure()` does not add the observability helpers. Call `AddObservabilityTesting()` separately when needed.

```csharp
var services = new ServiceCollection();
services
    .AddMvpTestingInfrastructure()
    .AddObservabilityTesting("Mvp24Hours.*");

using ServiceProvider provider = services.BuildServiceProvider();
MockClock clock = provider.GetRequiredService<MockClock>();
IFakeEmailService email = provider.GetRequiredService<IFakeEmailService>();
TestHttpMessageHandler http = provider.GetRequiredService<TestHttpMessageHandler>();
```

`ReplaceWithTestInfrastructure` is useful with an existing application registration:

```csharp
services.ReplaceWithTestInfrastructure(options =>
{
    options.InitialClockTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    options.EmailShouldFail = true;
    options.UseFakeSms = false;
});
```

### `TestInfrastructureOptions`

| Property | Type | Default | Meaning |
|---|---|---|---|
| `UseMockClock` | `bool` | `true` | Register `MockClock` |
| `InitialClockTime` | `DateTime?` | `null` | Initial UTC clock time |
| `UseFakeEmail` | `bool` | `true` | Register fake email |
| `EmailShouldFail` | `bool` | `false` | Make fake email return failure |
| `UseFakeSms` | `bool` | `true` | Register fake SMS |
| `SmsShouldFail` | `bool` | `false` | Make fake SMS return failure |
| `UseFakeFileStorage` | `bool` | `true` | Register fake file storage |
| `UseFakeHttp` | `bool` | `true` | Register the HTTP test handler |

## Time

`MockClock` implements `IClock`. Construct it with an initial UTC `DateTime`, or use `FromYear`, `FromDate`, `FromDateTime`, or `FromNow`. Tests can call `SetUtcNow`, `AdvanceBy`, `RewindBy`, the seconds/minutes/hours/days convenience methods, and `Reset`.

```csharp
MockClock clock = MockClock.FromDateTime(2026, 7, 1, 9);

DateTime before = clock.UtcNow;
clock.AdvanceHours(3);

Assert.Equal(before.AddHours(3), clock.UtcNow);
clock.Reset();
Assert.Equal(before, clock.UtcNow);
```

For APIs based on `TimeProvider`, `FakeTimeProviderHelper.FromClock(clock)` adapts an `IClock`, `ToClock(timeProvider)` adapts in the other direction, and `FixedAt(...)` returns a fixed provider. For timer-aware virtual time, use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`; `FixedAt` does not advance timers.

## Email, SMS, and file storage fakes

All three fakes record calls, are safe to query while tests run concurrently, and support reset methods. Configure each fake before invoking the system under test.

```csharp
services.AddFakeEmailService(fake =>
{
    fake.ShouldFail = false;
    fake.SimulatedDelay = TimeSpan.FromMilliseconds(5);
});

IFakeEmailService email = provider.GetRequiredService<IFakeEmailService>();
await email.SendAsync(new EmailMessage
{
    To = ["customer@example.com"],
    Subject = "Order accepted",
    PlainTextBody = "Your order was accepted."
});

EmailAssertions.AssertEmailSentTo(email, "customer@example.com");
EmailAssertions.AssertEmailSentWithSubject(email, "Order accepted");
```

| Fake | Configuration | Recorded state and helpers |
|---|---|---|
| `FakeEmailService` | `ShouldFail`, `FailureMessage`, `SimulatedDelay`, `CustomResultFactory` | `SentEmails`, `GetLastSentEmail`, `GetEmailsSentTo`, `WasEmailSentWithSubject`, `ClearSentEmails` |
| `FakeSmsService` | `ShouldFail`, `FailureMessage`, `SimulatedDelay`, `CustomResultFactory` | `SentMessages`, `SentMmsMessages`, recipient/text queries, `ClearSentMessages` |
| `FakeFileStorage` | `ShouldUploadFail`, `ShouldDownloadFail`, `FailureMessage`, `SimulatedDelay`, custom upload/download factories | `StoredFilePaths`, `FileCount`, `SeedFile`, `HasFile`, `GetFileContent`, `ClearFiles` |

`FakeFileStorage` implements byte, stream, and chunk upload/download paths plus metadata, list, copy, move, exists, and delete operations. It normalizes `\` to `/` and trims leading/trailing separators. It is an in-memory behavioral double, not a substitute for testing Azure Blob or local-file-system semantics.

The matching assertion classes are `EmailAssertions`, `SmsAssertions`, and `FileStorageAssertions`. They throw `Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException` and do not depend on a third-party assertion library.

## HTTP clients

`TestHttpMessageHandler` returns `200 OK` with `{}` unless configured. The first matching `When` rule wins.

```csharp
var handler = new TestHttpMessageHandler()
    .WhenGet("/customers/42", HttpStatusCode.OK, new { id = 42 })
    .WhenPost("/customers", HttpStatusCode.Created, new { id = 43 });

using var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://example.test")
};

HttpResponseMessage response = await client.GetAsync("/customers/42");

HttpAssertions.AssertGetRequestMade(handler, "/customers/42");
HttpAssertions.AssertRequestCount(handler, 1);
RecordedRequest request = HttpAssertions.GetLastRequest(handler);
```

Responses can be configured with `RespondWith`, `When`, `WhenUrl`, `WhenGet`, `WhenPost`, `WhenPut`, or `WhenDelete`. Failure paths use `ThrowException`, `SimulateTimeout`, and `SimulateNetworkFailure`. Every request captures method, URI, headers, body, and timestamp; `RecordedRequest.GetBodyAs<T>()`, `GetHeader`, and `HasHeader` simplify inspection. Call `ClearRequests` and `ClearMatchers` between shared-fixture tests.

`HttpClientTestFixture` wraps the same handler and provides client creation, common response setup, failure setup, request counts, and `Reset`.

## Logging, traces, and metrics

Use `FakeLogger<T>` for one directly injected logger. Use `InMemoryLoggerProvider` to capture all categories produced through DI. Note that `AddInMemoryLoggerProvider()` calls `ClearProviders()`.

```csharp
var logger = new FakeLogger<OrderService>();
var service = new OrderService(logger);

await service.ProcessAsync();

LogAssertions.AssertLogged(logger, LogLevel.Information, "processed");
LogAssertions.AssertNoErrorsLogged(logger);
```

`FakeActivityListener` records an activity when it stops, so duration, final status, tags, events, links, and baggage are available. `FakeMeterListener` records numeric measurements (`byte`, `short`, `int`, `long`, `float`, `double`, and `decimal`). Both filters support either an exact source/meter name or a trailing `*` prefix filter.

```csharp
using var activityListener = new FakeActivityListener("MyCompany.*");
using var meterListener = new FakeMeterListener("MyCompany.*");

await service.ProcessAsync();

ActivityAssertions.AssertActivityRecorded(activityListener, "orders.process");
ActivityAssertions.AssertActivityHasTag(
    activityListener, "orders.process", "tenant.id", "acme");
MetricAssertions.AssertMetricRecorded(meterListener, "orders.processed");
MetricAssertions.AssertCounterValueAtLeast(meterListener, "orders.processed", 1);
```

Available assertion groups:

| Class | Checks |
|---|---|
| `LogAssertions` | Message/level/category, count, exception type, and absence of warnings/errors |
| `ActivityAssertions` | Operation/source/count, tags, events, kind, duration, errors, and parent-child relationship |
| `MetricAssertions` | Instrument/meter, counter sum, count, tags, average/range, and individual values |
| `HttpAssertions` | Method/URL, count, headers, body, and absence of requests |
| `EmailAssertions`, `SmsAssertions`, `FileStorageAssertions` | Recorded side effects and content |

`ObservabilityTestFixture` combines the provider and listeners, exposes `Reset`, and can add services before its provider is first accessed. `InfrastructureTestFixture` similarly combines a test clock, HTTP handler, and communication/storage fakes. `InfrastructureTestFixture` exposes `TestClock` (the core `TestClock` type), while the DI extension registers `MockClock`.

Always dispose activity and meter listeners. They subscribe globally to .NET diagnostics and an undisposed unfiltered listener can capture unrelated parallel tests.

## EF Core helpers

Import `Mvp24Hours.Infrastructure.Data.EFCore.Testing`.

### DI registrations

| Method | Purpose |
|---|---|
| `AddMvp24HoursInMemoryDbContext<TContext>(databaseName?, configureOptions?)` | Registers the context with EF Core InMemory |
| `AddMvp24HoursUniqueInMemoryDbContext<TContext>(prefix?)` | Uses a GUID-suffixed database name |
| `AddMvp24HoursFakeRepository()` / `AddMvp24HoursFakeRepositoryAsync()` | Registers fake unit of work and open-generic repository |
| `AddMvp24HoursFakeRepositoryWithData<TEntity>(seed)` | Registers a seeded sync fake for one entity |
| `AddMvp24HoursFakeRepositoryAsyncWithData<TEntity>(seed)` | Registers a seeded async fake for one entity |
| `AddMvp24HoursTestDbContextFactory<TContext>(configure?)` | Registers `ITestDbContextFactory<TContext>` backed by EF InMemory |
| `AddMvp24HoursInMemoryDbContextFactory<TContext>(configure?)` | Registers `InMemoryDbContextFactory<TContext>` |
| `AddMvp24HoursTestInfrastructure<TContext>(databaseName?)` | Registers InMemory context plus Mvp24Hours async repository infrastructure |
| `AddMvp24HoursTestInfrastructureWithSeeder<TContext,TSeeder>()` | Adds the preceding setup and an `IDataSeeder<TContext>` |

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddMvp24HoursTestInfrastructure<AppDbContext>("OrdersTest");

using ServiceProvider provider = services.BuildServiceProvider();
using IServiceScope scope = provider.CreateScope();
AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
IUnitOfWorkAsync unitOfWork =
    scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
```

For a domain-only test, a fake repository avoids EF entirely:

```csharp
using var repository = new RepositoryFake<Customer>();
repository.SeedData([existingCustomer]);

repository.Add(newCustomer);
Assert.Single(repository.PendingAdds);
Assert.Equal(1, repository.CommitChanges());
```

`RepositoryFake<TEntity>` and `RepositoryFakeAsync<TEntity>` keep committed entities and pending adds/modifies/removes in memory. They support the repository query surface, `SeedData`, `ResetPendingChanges`, and commit operations. Relationship-loading methods are no-ops; provider translation, relational constraints, concurrency, and transaction behavior are not reproduced.

### `InMemoryDbContextOptions`

| Property | Default | Notes |
|---|---|---|
| `DatabaseName` | `null` | Base name; defaults to `InMemoryTestDb` |
| `UseUniqueDatabaseName` | `true` | Appends a GUID |
| `EnableSensitiveDataLogging` | `true` | Test diagnostics only |
| `EnableDetailedErrors` | `true` | Enables detailed EF errors |
| `SuppressTransactionWarning` | `true` | Ignores InMemory transaction warnings |
| `ThrowOnClientEvaluationWarning` | `false` | Configures warning behavior where supported |
| `EnforceForeignKeys` | `false` | Option is exposed, but EF InMemory does not enforce relational foreign keys |
| `ConfigureOptions` | `null` | Additional builder action |
| `ConfigureWarnings` | `null` | Additional warning action |
| `ValidateModel` | `true` | Requests model validation on context creation |

`InMemoryDbContextFactory<TContext>` can create a plain context, call `EnsureCreated`, or seed through `IDataSeeder<TContext>`/an action. `InMemoryDbContextHelper` provides one-call context and options creation.

`TestDbContextFactoryOptions` is the lower-level factory configuration:

| Property | Default |
|---|---|
| `ConnectionString` | `null` |
| `UseMigrations` | `false` |
| `CreateNewDatabasePerTest` | `true` |
| `DatabaseNamePrefix` | `"TestDb_"` |
| `EnableSensitiveDataLogging` | `true` |
| `EnableDetailedErrors` | `true` |
| `Interceptors` | empty |
| `ConfigureOptions` | `null` |

The concrete factory currently registered by `AddMvp24HoursTestDbContextFactory` is `InMemoryTestDbContextFactory<TContext>`; its connection-string property is therefore not used to select a relational provider.

## MongoDB helpers

Import `Mvp24Hours.Infrastructure.Data.MongoDb.Testing`.

### In-process fakes

`MongoDbInMemoryProvider` owns named `InMemoryMongoCollection<TEntity>` instances. Collections implement insert, replace, delete, ID lookup, predicate lookup, count, and clear operations in-process. They are not `IMongoCollection<TEntity>` and do not emulate MongoDB query translation, indexes, sessions, transactions, or server validation.

```csharp
using var mongo = new MongoDbInMemoryProvider(
    MongoDbInMemoryOptions.ForUnitTesting());

InMemoryMongoCollection<Customer> customers =
    mongo.GetCollection<Customer>("customers");
customers.InsertOne(customer);

Assert.Same(customer, customers.FindById(customer.EntityKey));
```

The Mongo repository fakes mirror the sync/async EF fake pattern through `MongoRepositoryFake<TEntity>`, `MongoRepositoryFakeAsync<TEntity>`, `MongoUnitOfWorkFake`, and `MongoUnitOfWorkFakeAsync`.

| DI method | Purpose |
|---|---|
| `AddMvp24HoursMongoFakeRepository()` / `...Async()` | Open-generic fake repository and fake unit of work |
| `AddMvp24HoursMongoFakeRepositoryWithData<TEntity>(seed)` | Seeded sync entity fake |
| `AddMvp24HoursMongoFakeRepositoryAsyncWithData<TEntity>(seed)` | Seeded async entity fake |
| `AddMvp24HoursMongoInMemoryProvider(configure?)` | Registers `MongoDbInMemoryProvider` |
| `AddMvp24HoursMongoContextFactory(connectionString, configure?)` | Registers `MongoDbContextFactory` |
| `AddMvp24HoursMongoTestInfrastructure(connectionString, configure?)` | Registers a real Mongo context and async repository against that connection |
| `AddMvp24HoursMongoTestInfrastructureWithSeeder<TSeeder>(connectionString)` | Adds `IMongoDataSeeder` |
| `AddMvp24HoursMongoFakeTestInfrastructure()` | Registers both sync and async repository fakes |

### `MongoDbInMemoryOptions`

| Property | Default |
|---|---|
| `DatabaseNamePrefix` | `"InMemoryMongoTestDb"` |
| `DatabaseName` | `null` |
| `UseUniqueDatabaseName` | `true` |
| `ConnectionString` | `null` |
| `EnableLogging` | `true` |
| `EnableTransaction` | `false` |
| `EnableMultiTenancy` | `false` |
| `TimeoutSeconds` | `30` |
| `ConfigureOptions` | `null` |

Presets are `ForUnitTesting()` (logging off, transactions off, 5-second timeout), `ForIntegrationTesting()` (logging on, 60-second timeout), and `ForSharedDatabase(name)`.

### Testcontainers

`MongoDbTestcontainersHelper` does **not** reference Testcontainers and does not create, start, stop, or dispose a container. The currently implemented public methods:

- create `MongoDbOptions`, `Mvp24HoursContext`, or `MongoDbContextFactory` from a connection string;
- check Docker availability by running `docker version`;
- poll MongoDB with `WaitForMongoDbReadyAsync`;
- drop non-system collections with `CleanDatabaseAsync`.

Create the container in the test project, then pass its connection string to the helper:

```csharp
await using var container = new MongoDbBuilder("mongo:6.0").Build();
await container.StartAsync();

MongoDbTestcontainersOptions options =
    MongoDbTestcontainersOptions.ForBasicTesting();
bool ready = await MongoDbTestcontainersHelper.WaitForMongoDbReadyAsync(
    container.GetConnectionString());
Assert.True(ready);

using Mvp24HoursContext context = MongoDbTestcontainersHelper.CreateContext(
    container.GetConnectionString(), options);
```

The example above requires `Testcontainers.MongoDb` and `using Testcontainers.MongoDb;`.

| `MongoDbTestcontainersOptions` property | Default | Notes |
|---|---|---|
| `ImageTag` | `"latest"` | Used by `GetImageName`; presets use `"6.0"` |
| `DatabaseName` | `"testdb"` | Base database name |
| `UseUniqueDatabaseName` | `true` | Appends a GUID |
| `Port` | `null` | Metadata only; the helper does not build a container |
| `Username` / `Password` | `null` | Metadata only |
| `EnableReplicaSet` | `false` | Maps to `MongoDbOptions.EnableTransaction` |
| `StartupTimeoutSeconds` | `60` | Maps to connection/socket timeout |
| `AutoRemove` | `true` | Metadata only |
| `ContainerNamePrefix` | `"mvp24hours-mongodb-test"` | Metadata only |

`ForBasicTesting`, `ForAuthenticatedTesting`, and `ForReplicaSetTesting` create option presets. Authentication, image, port, replica-set startup, auto-removal, and container naming must still be applied to the container builder by the test.

## RabbitMQ in-memory bus and harness

Import:

```csharp
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Helpers;
```

| Registration | Purpose |
|---|---|
| `AddInMemoryRabbitMQ()` | Registers singleton `IInMemoryBus` and exposes it as `IMvpRabbitMQClient` |
| `AddRabbitMQTestHarness()` | Adds the in-memory bus and singleton `ITestHarness` |
| `AddRabbitMQTestHarness(configure)` | Optionally scans assemblies for `IMessageConsumer<T>` implementations |
| `ReplaceRabbitMQWithInMemory()` | Removes existing `IMvpRabbitMQClient` registrations and installs the bus |
| `AddTestConsumer<TConsumer>()` | Registers a consumer and its message-consumer interfaces |
| `AddTestRequestHandler<THandler>()` | Registers a request handler and its request interfaces |

```csharp
var services = new ServiceCollection();
// OrderCreated and OrderCreatedConsumer belong to the application under test.
services.AddTestConsumer<OrderCreatedConsumer>();
services.AddRabbitMQTestHarness();

using ServiceProvider provider = services.BuildServiceProvider();
ITestHarness harness = provider.GetRequiredService<ITestHarness>();
await harness.StartAsync();

IConsumedMessage<OrderCreated> consumed =
    await harness.PublishAndWaitAsync(new OrderCreated(orderId));

Assert.True(consumed.IsSuccess);
harness.Bus.AssertSinglePublished<OrderCreated>();
harness.Bus.AssertSingleConsumed<OrderCreated>();
```

`IInMemoryBus` tracks published and consumed messages, supports predicates and counts, and can clear state. `ConsumeAsync` resolves registered `IMessageConsumer<T>` implementations. Failure simulation includes delay, timeout, arbitrary exception, network failure, and broker-unavailable helpers. `TestConsumeContextBuilder<T>` configures IDs, exchange, routing key, queue, headers, redelivery, sent time, service provider, tenant, user, and cancellation.

`ITestHarness` adds start/stop, publish-and-wait, request/response, consumer-specific harnesses, wait-for-publish/consume, and `Reset`. `TestHarnessBuilder` is an alternative fluent constructor for services, consumers, request handlers, assembly scanning, and an in-memory bus.

`TestHarnessOptions` contains `AutoRegisterConsumers` (default `false`) and `ConsumerAssemblies` (empty). `AddConsumersFromAssembly` and `AddConsumersFromAssemblyContaining<T>` enable scanning.

The in-memory bus executes consumers in-process. It does not validate RabbitMQ connectivity, serialization, exchange/queue declaration, acknowledgements from a real channel, dead lettering, publisher confirms, broker permissions, or network recovery. Use a real RabbitMQ container for those integration tests; the repository's container-based RabbitMQ tests create that container outside these helpers.

## ASP.NET Core integration tests

For complete HTTP-pipeline tests, use `WebApplicationFactory<TEntryPoint>` and replace external services in `ConfigureTestServices`:

```csharp
factory = factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureTestServices(services =>
    {
        services.ReplaceWithTestInfrastructure(options =>
        {
            options.InitialClockTime =
                new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        });
    });
});
```

This validates routing, middleware, serialization, and DI while keeping email, SMS, storage, time, and outbound HTTP deterministic. Use provider-specific test infrastructure when database or broker semantics are part of the behavior.

### Domain and application unit-test sketch

```csharp
[Fact]
public void Order_AddItem_IncreasesTotal()
{
    var order = new Order(customerId: 10);

    order.AddItem(productId: 3, quantity: 2, unitPrice: 12.5m);

    Assert.Equal(25m, order.Total);
}

[Fact]
public async Task CreateOrder_WhenCustomerMissing_ReturnsFailure()
{
    var customers = Substitute.For<ICustomerReadModel>();
    customers.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);
    var handler = new CreateOrderHandler(customers, /* ... */);

    var result = await handler.Handle(
        new CreateOrderCommand(99, []),
        CancellationToken.None);

    Assert.True(result.HasErrors);
}
```

Use Mvp24Hours fakes for infrastructure ports first. Reach for NSubstitute only when the dependency is an application-owned contract without a library fake.

## Related

- [Getting Started](../getting-started.md)
- [Infrastructure Modules](../infrastructure/home.md)
- [Configuration Reference](../configuration-reference.md)
- [Architecture Guides](../guides/architecture/home.md)
- [EF Core Advanced](../database/efcore-advanced.md)
- [MongoDB Advanced](../database/mongodb-advanced.md)
- [Message Broker](../broker.md)
- [RabbitMQ Advanced Features](../broker-advanced.md)
- [Observability](../observability/home.md)
- [Logging](../observability/logging.md)
- [Tracing](../observability/tracing.md)
- [Metrics](../observability/metrics.md)
- [xUnit](https://xunit.net/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
