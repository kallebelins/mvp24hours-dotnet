# Banco de Dados NoSQL
>NoSQL (originalmente se referindo a "no SQL": "não SQL" ou "não relacional", posteriormente estendido para Not Only SQL - Não Somente SQL) é um termo genérico que representa os bancos de dados não relacionais. Uma classe definida de banco de dados que fornecem um mecanismo para armazenamento e recuperação de dados que são modelados de formas diferentes das relações tabulares usadas nos bancos de dados relacionais. [Wikipédia](https://pt.wikipedia.org/wiki/NoSQL)

## Orientado a Documento
> Um banco de dados orientado a documentos, ou armazenamento de documentos, é um programa de computador e sistema de armazenamento de dados projetado para armazenar, recuperar e gerenciar informações orientadas a documentos, também conhecido como dados semiestruturados. [Wikipedia](https://en.wikipedia.org/wiki/Document-oriented_database)

Foi implementado padrão de repositório com critérios de pesquisa e paginação, além de unidade de trabalho ([Veja Repositório](pt-br/database/use-repository)). Esta implementação não oferece suporte apenas a carga tardia de objetos relacionados. 

### MongoDB

#### Pré-Requisitos (Não Obrigatório)
Adicione um arquivo de configuração ao projeto com nome "appsettings.json". O arquivo deverá conter um chave com dados de conexão, por exemplo, ConnectionStrings/DataContext conforme abaixo:
```json
{
  "ConnectionStrings": {
    "DataContext": "String de conexão"
  }
}
```
Você poderá usar a conexão de banco de dados direto, o que não é recomendado. Acesse o site [ConnectionStrings](https://www.connectionstrings.com/) e veja como montar a conexão com seu banco.

#### Instalação
```csharp
/// Package Manager Console >

Install-Package MongoDB.Driver -Version 2.13.2
Install-Package Mvp24Hours.Infrastructure.Data.MongoDb -Version 8.2.102
```
#### Configuração
```csharp
/// Startup.cs
services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = Configuration.GetConnectionString("DataContext");
});
services.AddMvp24HoursRepository(); // async => AddMvp24HoursRepositoryAsync()

```

#### Usando Docker
**Comando Básico**
```
// Command
docker run -d --name mongo -p 27017:27017 mvertes/alpine-mongo

// ConnectionString
mongodb://localhost:27017

```

**Comando para Banco com Senha**
```
// Command
docker run --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=123456 mongo

// ConnectionString
mongodb://user:123456@localhost:27017

```

## Orientado por Chave-Valor
> Um banco de dados de chave-valor, ou armazenamento de chave-valor, é um paradigma de armazenamento de dados projetado para armazenar, recuperar e gerenciar matrizes associativas e uma estrutura de dados mais comumente conhecida hoje como dicionário ou tabela hash. [Wikipédia](https://pt.wikipedia.org/wiki/Banco_de_dados_de_chave-valor)

### Redis
Estrutura de dados em memória, usado como um banco de dados distribuído de chave-valor, cache e agente de mensagens.

#### Pré-Requisitos (Não Obrigatório)
Adicione um arquivo de configuração ao projeto com nome "appsettings.json", conforme abaixo:
```json
{
  "ConnectionStrings": {
    "RedisDbContext": null
  }
}

```
Você poderá usar configuração estrutural ou string de conexão.

#### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.Caching.Redis -Version 8.2.102
```

#### Configuração
```csharp
/// Startup.cs

// structural
services.AddMvp24HoursCaching();

// connection string
services.AddMvp24HoursCachingRedis(Configuration.GetConnectionString("RedisDbContext"));

```

#### Exemplo de Uso
Você poderá usar o Redis para registrar valor simples ou objetos complexos, assim:


```csharp
// obtém cache
var cache = serviceProvider.GetService<IDistributedCache>();

// objeto de referência
var customer = new Customer
{
    Oid = Guid.NewGuid(),
    Created = DateTime.Now,
    Name = "Test 1",
    Active = true
};

// adicionar valor simples
string content = customer.ToSerialize();
cache.SetString("key", content);

// recuperar valor simples
string content = cache.GetString("key");

// remover valor simples
cache.Remove("key");

// adicionar valor complexo
cache.SetObject("key", customer);

// recuperar valor complexo
var customer = cache.GetObject<Customer>("key");

// remover valor complexo
cache.Remove("key");

```

Você poderá usar extensions para interagir com a interface IDistributedCache no namespace "Mvp24Hours.Infrastructure.Extensions".

Você ainda poderá usar o conceito de repositório para restringir os tipos exclusivos para uso.

```csharp
/// Startup.cs
services.AddScoped<IRepositoryCache<Customer>, RepositoryCache<Customer>>();

// objeto de referência
var customer = new Customer
{
    Oid = Guid.NewGuid(),
    Created = DateTime.Now,
    Name = "Test 1",
    Active = true
};

// adicionar no formato texto
string content = customer.ToSerialize();
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.SetString("key", content);

// recuperar no formato texto
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
string content = repo.GetString("key");

// remover
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.Remove(_keyString);

// adicionar valor complexo
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.Set("key", customer);

// recuperar valor complexo
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
var customer = repo.Get("key");

```

#### Usando Docker
```
// Command
docker run -d -p 6379:6379 -i -t redis:3.2.5-alpine

// Connect
127.0.0.1:6379

```
