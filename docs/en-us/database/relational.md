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

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 5.0.12
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Settings
```csharp
/// Startup.cs

services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DataContext")));

services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted; // Serializable, RepeatableRead, ReadUncommitted, Snapshot, Chaos
});  // async => services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -p 1433:1433 -d mcr.microsoft.com/mssql/server

// ConnectionString
Data Source=.,1433;Initial Catalog=MyTestDb;Persist Security Info=True;User ID=sa;Password=MyPass@word;Pooling=False;

```

## PostgreSql
### Setup
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL -Version 5.0.10
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Settings
```csharp
/// Startup.cs

services.AddDbContext<DataContext>(
    options => options.UseNpgsql(Configuration.GetConnectionString("DataContext"),
    options => options.SetPostgresVersion(new Version(9, 6)))
);

services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted; // Serializable, RepeatableRead, ReadUncommitted, Snapshot, Chaos
});  // async => services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=MyPass@word -d onjin/alpine-postgres

// ConnectionString
Host=localhost;Port=5432;Pooling=true;Database=MyTestDb;User Id=postgres;Password=MyPass@word;

```

## MySql
### Setup
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package MySql.EntityFrameworkCore -Version 5.0.8
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Settings
```csharp
/// Startup.cs

services.AddDbContext<DataContext>(options =>
    options.UseMySQL(Configuration.GetConnectionString("DataContext")));

services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted; // Serializable, RepeatableRead, ReadUncommitted, Snapshot, Chaos
});  // async => services.AddMvp24HoursRepositoryAsync();

```
### Using Docker
```
// Command
docker run --name mysql -v /mysql/data/:/var/lib/mysql -d -p 3306:3306 -e MYSQL_ROOT_PWD=MyPass@word -e MYSQL_USER=user -e MYSQL_USER_PWD=MyPass@word leafney/docker-alpine-mysql

// ConnectionString
server=localhost;user=root;password=MyPass@word;database=MyTestDb

```