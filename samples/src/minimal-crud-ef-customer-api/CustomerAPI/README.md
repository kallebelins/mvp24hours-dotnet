# CustomerAPI - CRUD - EF - Minimal API
Minimized API design much simpler than usual.

## Features:
- Relational database (SQL Server, PostgreSql, MySql) with EF; 
- Documentation (Swagger); 
- Logging (NLog); 
- Patterns for data validation (FluentValidation and Data Annotations);
- Unit of Work (Transaction);
- Repository (Paging, List, Create, Update, Delete) - Query apply: Navigation, Filter, Paging;
- FluentAPI configuration EF;
- Dependency injection (IoC);
- Using ExtensionBinder and ModelBinder for API resources (Restful);
- Middlewares for handling unmanaged failures;
- DDD concepts;
- Health Checks;

## Database integrated with EF

### SqlServer

```csharp
/// Package Manager Console >
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 5.0.10

/// Startup.cs
services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DataContext"))
);
```

Access: https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/relational?id=sql-server

### PostgreSQL

```csharp
/// Package Manager Console >
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL -Version 5.0.10

/// Startup.cs
services.AddDbContext<DataContext>(
    options => options.UseNpgsql(configuration.GetConnectionString("DataContext"),
    options => options.SetPostgresVersion(new Version(9, 6)))
);
```

Access: https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/relational?id=postgresql

### MySql

```csharp
/// Package Manager Console >
Install-Package MySql.EntityFrameworkCore -Version 5.0.8

/// Startup.cs
services.AddDbContext<DataContext>(options =>
    options.UseMySQL(configuration.GetConnectionString("EFDBContext"))
);
```

Access: https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/database/relational?id=mysql

## Health Check

```csharp
/// Package Manager Console >
Install-Package AspNetCore.HealthChecks.UI.Client -Version 3.1.2
```

Access: https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks#health-checks

### SqlServer

```csharp
/// Package Manager Console >
Install-Package AspNetCore.HealthChecks.SqlServer -Version 3.2.0

/// ServiceBuilderExtensions
services.AddHealthChecks()
	.AddSqlServer(
		configuration.GetConnectionString("EFDBContext"),
		healthQuery: "SELECT 1;",
		name: "SqlServer", 
		failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

```

### PostgreSQL

```csharp
/// Package Manager Console >
Install-Package AspNetCore.HealthChecks.Npgsql -Version 3.1.1

/// ServiceBuilderExtensions
services.AddHealthChecks()
	.AddNpgSql(
		configuration.GetConnectionString("EFDBContext"),
		name: "PostgreSql", 
		failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

```

### MySql

```csharp
/// Package Manager Console >
Install-Package AspNetCore.HealthChecks.MySql -Version 3.2.0

/// ServiceBuilderExtensions
services.AddHealthChecks()
	.AddMySql(
		configuration.GetConnectionString("EFDBContext"), 
		name: "MySql", 
		failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
```
