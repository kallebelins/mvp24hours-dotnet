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

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -p 1433:1433 -d mcr.microsoft.com/mssql/server

// ConnectionString
Data Source=.,1433;Initial Catalog=MyTestDb;Persist Security Info=True;User ID=sa;Password=MyPass@word;Pooling=False;TrustServerCertificate=True

```

## PostgreSql
### Instalação
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=MyPass@word -d postgres:16-alpine

// ConnectionString
Host=localhost;Port=5432;Pooling=true;Database=MyTestDb;User Id=postgres;Password=MyPass@word;

```

## MySql
### Instalação
```csharp
/// Package Manager Console >

Install-Package Microsoft.Extensions.DependencyInjection -Version 9.0.0
Install-Package Pomelo.EntityFrameworkCore.MySql -Version 9.0.0
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```
### Configuração
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
### Usando Docker
```
// Command
docker run --name mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=MyPass@word -d mysql:8

// ConnectionString
server=localhost;user=root;password=MyPass@word;database=MyTestDb

```

---

## Integração com Observabilidade

Habilite health checks e métricas para seu banco de dados:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DataContext"), name: "sqlserver");

// ou para PostgreSQL
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DataContext"), name: "postgres");
```

---

## Documentação Relacionada

- [EF Core Avançado](efcore-advanced.md) - Recursos avançados, estratégias de execução, resiliência
- [Usar Entidade](use-entity.md) - Configuração de entidade
- [Usar Repositório](use-repository.md) - Uso do padrão repositório
- [Usar Unit of Work](use-unitofwork.md) - Padrão Unit of Work
