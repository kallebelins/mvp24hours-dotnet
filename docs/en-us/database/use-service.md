# Repository and Application Services

The Application package provides two service families:

- `RepositoryService<TEntity, TUoW>` and `RepositoryServiceAsync<TEntity, TUoW>` expose repository-oriented query and command methods that return `IBusinessResult<T>`.
- `ApplicationServiceBase<TEntity, TUoW>` and `ApplicationServiceBaseAsync<TEntity, TUoW>` are extensible application-layer bases with validation and logging support. DTO and separate create/update DTO variants are also available.

## Repository service

This is the pattern exercised by the SQL and MongoDB application tests:

```csharp
public sealed class CustomerService(IUnitOfWorkAsync unitOfWork)
    : RepositoryServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork)
{
    public Task<IBusinessResult<IList<Customer>>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return GetByAsync(customer => customer.Active, cancellationToken: cancellationToken);
    }
}
```

Register the provider, repository, and concrete service with the same scoped lifetime:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddMvp24HoursDbContext<AppDbContext>();
builder.Services.AddMvp24HoursRepositoryAsync();
builder.Services.AddScoped<CustomerService>();
```

For MongoDB:

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = mongoConnectionString;
});
builder.Services.AddMvp24HoursRepositoryAsync(repositoryOptions: null);
builder.Services.AddScoped<CustomerService>();
```

The base service exposes list/count/exists/get-by-id queries and add/modify/remove commands. `RepositoryPagingService<TEntity, TUoW>` and `RepositoryPagingServiceAsync<TEntity, TUoW>` add paginated result methods.

## Application service base

Use an application service base when the application boundary needs validation, logging, DTO mapping, or separately modeled create/update inputs:

```csharp
public sealed class CustomerApplicationService(
    IUnitOfWorkAsync unitOfWork,
    IValidator<Customer> validator,
    ILogger<CustomerApplicationService> logger)
    : ApplicationServiceBaseAsync<Customer, IUnitOfWorkAsync>(
        unitOfWork,
        validator,
        logger);
```

Available variants include:

- `ApplicationServiceBaseWithDto` / `ApplicationServiceBaseWithDtoAsync`
- `ApplicationServiceBaseWithSeparateDtos` / `ApplicationServiceBaseWithSeparateDtosAsync`
- `QueryServiceBase` / `QueryServiceBaseAsync`
- `EventAwareCommandServiceBaseAsync`
- `CacheableApplicationServiceBaseAsync`

Choose one base and add domain-specific methods in the concrete service. Keep transaction boundaries in the service or handler, and inject services rather than resolving them from `IServiceProvider`.

See [Application Services](../application-services.md), [Repository](use-repository.md), and [Unit of Work](use-unitofwork.md).
