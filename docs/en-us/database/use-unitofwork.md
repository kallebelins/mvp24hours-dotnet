# How to use a unit of work?
We use the unit of work standard to control transactions. According to Martin Fowler:
> Maintains a list of objects affected by a business transaction and coordinates recording changes and resolving concurrency issues. [Unit Of Work](http://martinfowler.com/eaaCatalog/unitOfWork.html)

## Prerequisites
Perform installation and configuration to use a [relational](relational.md) or [NoSQL](nosql.md) database.

# Unit of Work
To obtain this, simply apply the injection concept through the constructor or use the Mvp24Hours architecture help provider, like this:
```csharp
IUnitOfWork unitOfWork = serviceProvider.GetService<IUnitOfWork>(); // async => IUnitOfWorkAsync
```

## Predefined Methods
```csharp
// IUnitOfWork / IUnitOfWorkAsync
int SaveChanges(CancellationToken cancellationToken = default); // async => SaveChangesAsync
void Rollback(); // async => RollbackAsync
IRepository<T> GetRepository<T>() where T : class, IEntityBase;
IDbConnection GetConnection();
```

## Using Dapper
Below is a package for installing and executing SQL commands/queries with Dapper.

```csharp
/// Package Manager Console >
Install-Package Dapper -Version 2.1.x

/// Example
var result = await UnitOfWork
    .GetConnection()
    .QueryAsync<Contact>("select * from Contact where CustomerId = @customerId;", new { customerId });
```