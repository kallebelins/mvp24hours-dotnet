# NoSQL Database
>NoSQL (originally referring to "no SQL": "non-SQL" or "non-relational", later extended to Not Only SQL) is a generic term representing non-relational databases. A defined class of database that provides a mechanism for storing and retrieving data that is modeled in ways other than the tabular relationships used in relational databases. [Wikipedia](https://pt.wikipedia.org/wiki/NoSQL)

## Document Oriented
> A document-oriented database, or document store, is a computer program and data storage system designed to store, retrieve, and manage document-oriented information, also known as semi-structured data. [Wikipedia](https://en.wikipedia.org/wiki/Document-oriented_database)

A repository pattern with search and pagination criteria was implemented, as well as a unit of work ([See Repository](use-repository.md)). This implementation does not support late loading of related objects only.

### MongoDB

#### Prerequisites (Not Mandatory)
Add a configuration file to the project named "appsettings.json". The file must contain a key with connection data, for example, ConnectionStrings/DataContext as below:
```json
{
  "ConnectionStrings": {
    "DataContext": "Connection string"
  }
}
```
You may be able to use direct database connection, which is not recommended. Access the website [ConnectionStrings](https://www.connectionstrings.com/) and see how to set up the connection with your database.

#### Setup
```csharp
/// Package Manager Console >

Install-Package MongoDB.Driver -Version 2.28.0
Install-Package Mvp24Hours.Infrastructure.Data.MongoDb -Version 9.1.x
```
#### Settings
```csharp
/// Program.cs
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = builder.Configuration.GetConnectionString("DataContext");
});
builder.Services.AddMvp24HoursRepository(); // async => AddMvp24HoursRepositoryAsync()

```

#### Using Docker
**Basic Command**
```
// Command
docker run -d --name mongo -p 27017:27017 mongo:7

// ConnectionString
mongodb://localhost:27017

```

**Command for Database with Password**
```
// Command
docker run --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=123456 mongo:7

// ConnectionString
mongodb://user:123456@localhost:27017

```

> 📚 See [MongoDB Advanced](mongodb-advanced.md) for advanced features and resilience configuration.

## Key-Value Oriented
> A key-value database, or key-value store, is a data storage paradigm designed to store, retrieve, and manage associative arrays and a data structure more commonly known today as a dictionary or hash table. [Wikipedia](https://pt.wikipedia.org/wiki/Banco_de_dados_de_chave-valor)

### Redis
In-memory data structure, used as a distributed key-value database, cache, and message broker.

#### Prerequisites (Not Mandatory)
Add a configuration file to the project with the name "appsettings.json", as follows:
```json
{
  "ConnectionStrings": {
    "RedisDbContext": null
  }
}

```
You can use structural configuration or connection string.

#### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.Caching.Redis -Version 9.1.x
```

#### Settings
```csharp
/// Program.cs

// structural
builder.Services.AddMvp24HoursCaching();

// connection string
builder.Services.AddMvp24HoursCachingRedis(builder.Configuration.GetConnectionString("RedisDbContext"));

```

#### Example of use
You can use Redis to register simple value or complex objects, like this:


```csharp
// get cache
var cache = serviceProvider.GetService<IDistributedCache>();

// reference object
var customer = new Customer
{
    Oid = Guid.NewGuid(),
    Created = DateTime.Now,
    Name = "Test 1",
    Active = true
};

// add simple value
string content = customer.ToSerialize();
cache.SetString("key", content);

// retrieve simple value
string content = cache.GetString("key");

// remove simple value
cache.Remove("key");

// add complex value
cache.SetObject("key", customer);

// retrieve complex value
var customer = cache.GetObject<Customer>("key");

// remove complex value
cache.Remove("key");

```

You will be able to use extensions to interact with the IDistributedCache interface in the "Mvp24Hours.Infrastructure.Extensions" namespace.

You can still use the repository concept to restrict the unique types for use.

```csharp
/// Program.cs
builder.Services.AddScoped<IRepositoryCache<Customer>, RepositoryCache<Customer>>();

// reference object
var customer = new Customer
{
    Oid = Guid.NewGuid(),
    Created = DateTime.Now,
    Name = "Test 1",
    Active = true
};

// add in text format
string content = customer.ToSerialize();
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.SetString("key", content);

// retrieve in text format
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
string content = repo.GetString("key");

// remove
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.Remove(_keyString);

// add complex value
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
repo.Set("key", customer);

// retrieve complex value
var repo = serviceProvider.GetService<IRepositoryCache<Customer>>();
var customer = repo.Get("key");

```

#### Using Docker
```
// Command
docker run -d -p 6379:6379 redis:7-alpine

// Connect
127.0.0.1:6379

```

---

## HybridCache (.NET 9+)

For modern caching with .NET 9+, consider using `HybridCache` which combines L1 (in-memory) and L2 (distributed) caching:

```csharp
// Program.cs
builder.Services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

// Usage
var item = await hybridCache.GetOrCreateAsync(
    "key",
    async cancel => await LoadDataAsync(cancel),
    new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    });
```

> 📚 See [HybridCache Documentation](../modernization/hybrid-cache.md) for complete guide.

---

## Observability Integration

Enable health checks for your NoSQL databases:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddMongoDb(builder.Configuration.GetConnectionString("DataContext"), name: "mongodb")
    .AddRedis(builder.Configuration.GetConnectionString("RedisDbContext"), name: "redis");
```
