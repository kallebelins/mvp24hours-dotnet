# Relational Database
>A relational database is a database that models data in such a way that it is perceived by the user as tables, or more formally relationships. [Wikipedia](https://pt.wikipedia.org/wiki/Banco_de_dados_relacional)

A repository standard was implemented with search and pagination criteria, in addition to a work unit. We use Entity Framework to perform persistence. The Entity Framework supports several databases and those that have already been tested are: PostgreSql, MySQL and SQLServer.

## Prerequisites (Not Mandatory)
Add a configuration file to the project named "appsettings.json". The file must contain a key with connection data, for example, ConnectionStrings/DataContext as below:
```json
{
  "ConnectionStrings": {
    "DataContext": "Connection string"
  }
}
```
You may be able to use direct database connection, which is not recommended. Access the website [ConnectionStrings](https://www.connectionstrings.com/) and see how to set up the connection with your database.

## SQL Server
### Setup
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Settings
```csharp
/// Program.cs

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")));

builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
});  // async => builder.Services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -p 1433:1433 -d mcr.microsoft.com/mssql/server

// ConnectionString
Data Source=.,1433;Initial Catalog=MyTestDb;Persist Security Info=True;User ID=sa;Password=MyPass@word;Pooling=False;TrustServerCertificate=True

```

## PostgreSql
### Setup
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Settings
```csharp
/// Program.cs

builder.Services.AddDbContext<DataContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("DataContext"))
);

builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
});  // async => builder.Services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=MyPass@word -d postgres:16-alpine

// ConnectionString
Host=localhost;Port=5432;Pooling=true;Database=MyTestDb;User Id=postgres;Password=MyPass@word;

```

## MySql
### Setup
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Pomelo.EntityFrameworkCore.MySql -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Settings
```csharp
/// Program.cs

builder.Services.AddDbContext<DataContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DataContext"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DataContext"))));

builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
});  // async => builder.Services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=MyPass@word -d mysql:8

// ConnectionString
server=localhost;user=root;password=MyPass@word;database=MyTestDb

```

---

## Observability Integration

Enable health checks and metrics for your database:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DataContext"), name: "sqlserver");

// or for PostgreSQL
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DataContext"), name: "postgres");
```

---

## Related Documentation

- [EF Core Advanced](efcore-advanced.md) - Advanced features, execution strategies, resilience
- [Use Entity](use-entity.md) - Entity configuration
- [Use Repository](use-repository.md) - Repository pattern usage
- [Use Unit of Work](use-unitofwork.md) - Unit of Work pattern
