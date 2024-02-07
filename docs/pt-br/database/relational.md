# Banco de Dados Relacional
>Um banco de dados relacional é um banco de dados que modela os dados de uma forma que eles sejam percebidos pelo usuário como tabelas, ou mais formalmente relações. [Wikipédia](https://pt.wikipedia.org/wiki/Banco_de_dados_relacional)

Foi implementado padrão de repositório com critérios de pesquisa e paginação, além de unidade de trabalho. Usamos Entity Framework para realizar persistência. O Entity Framework dá suporte a diversos bancos de dados e os que já foram testados são: PostgreSql, MySQL e SQLServer.

## Pré-Requisitos (Não Obrigatório)
Adicione um arquivo de configuração ao projeto com nome "appsettings.json". O arquivo deverá conter um chave com dados de conexão, por exemplo, ConnectionStrings/DataContext conforme abaixo:
```json
{
  "ConnectionStrings": {
    "DataContext": "String de conexão"
  }
}
```
Você poderá usar a conexão de banco de dados direto, o que não é recomendado. Acesse o site [ConnectionStrings](https://www.connectionstrings.com/) e veja como montar a conexão com seu banco.

## SQL Server
### Instalação
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 5.0.12
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -p 1433:1433 -d mcr.microsoft.com/mssql/server

// ConnectionString
Data Source=.,1433;Initial Catalog=MyTestDb;Persist Security Info=True;User ID=sa;Password=MyPass@word;Pooling=False;

```

## PostgreSql
### Instalação
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL -Version 5.0.10
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=MyPass@word -d onjin/alpine-postgres

// ConnectionString
Host=localhost;Port=5432;Pooling=true;Database=MyTestDb;User Id=postgres;Password=MyPass@word;

```

## MySql
### Instalação
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package MySql.EntityFrameworkCore -Version 5.0.8
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.2.101
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name mysql -v /mysql/data/:/var/lib/mysql -d -p 3306:3306 -e MYSQL_ROOT_PWD=MyPass@word -e MYSQL_USER=user -e MYSQL_USER_PWD=MyPass@word leafney/docker-alpine-mysql

// ConnectionString
server=localhost;user=root;password=MyPass@word;database=MyTestDb

```