// Aspire AppHost — local orchestrator for the Microservices-Aspire-Customer sample.
//
// Prerequisites:
//   dotnet workload install aspire
//   Docker (for SQL Server and RabbitMQ containers)
//
// Run: dotnet run --project AppHost/AppHost.csproj

var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ──────────────────────────────────────────────────────────

var sql = builder.AddSqlServer("sql")
    .WithDataVolume("aspire-customer-sql-data");

var customerDb = sql.AddDatabase("MyAspireCustomerDb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume("aspire-customer-rabbitmq-data");

// ── Services ─────────────────────────────────────────────────────────────────

// CustomerAPI: exposes HTTP endpoints, publishes CustomerCreatedEvent to RabbitMQ.
var customerApi = builder.AddProject<Projects.CustomerAPI>("customerapi")
    .WithReference(customerDb)
    .WithReference(rabbitmq)
    .WaitFor(customerDb)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints();

// NotificationWorker: consumes CustomerCreatedEvent from RabbitMQ, persists notification log.
builder.AddProject<Projects.NotificationWorker>("notificationworker")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WaitFor(customerApi);

builder.Build().Run();
