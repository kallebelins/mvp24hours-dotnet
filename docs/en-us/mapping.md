# Mapping
> A data mapper is a data access layer that performs bidirectional data transfer between a persistent data store (usually a relational database) and an in-memory data representation (the domain layer). [Wikipedia](https://en.wikipedia.org/wiki/Data_mapper_pattern)

## AutoMapper

### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure -Version 9.1.x
```

### Basic Settings
Implement the Mapping contract of the IMapFrom interface.

```csharp
/// CustomerResponse.cs
public class CustomerResponse : IMapFrom
{
    public string Name { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<Customer, CustomerResponse>();
}
```

#### Ignoring Properties
```csharp
public class TestIgnoreClass : IMapFrom
{
    public int MyProperty1 { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<TestAClass, TestIgnoreClass>()
            .MapIgnore(x => x.MyProperty1);
    }
}
```

#### Mapping New Properties
```csharp
public class TestPropertyClass : IMapFrom
{
    public int MyPropertyX { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<TestAClass, TestPropertyClass>()
            .MapProperty(x => x.MyProperty1, x => x.MyPropertyX);
    }
}
```

### Loading Settings
```csharp
/// Program.cs
builder.Services.AddMvp24HoursMapService(Assembly.GetExecutingAssembly());
```

### Basic Execution
```csharp
// setting
IMapper mapper = [mapperConfig].CreateMapper();

// running
var classA = new TestAClass { MyProperty1 = 1 };
var classB = mapper.Map<TestIgnoreClass>(classA);
```

## IMapFrom Interface

The `IMapFrom` interface is the recommended way to define mappings in the Mvp24Hours architecture. It allows you to define mappings directly in your DTOs/ViewModels, keeping mappings close to the classes they affect.

```csharp
/// <summary>
/// Interface for configuring AutoMapper mappings
/// </summary>
public interface IMapFrom
{
    /// <summary>
    /// Define the mapping configuration
    /// </summary>
    void Mapping(Profile profile);
}
```

### Alternative: IMapFrom<T>

For simpler mappings without custom configuration:

```csharp
public interface IMapFrom<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
}

// Usage
public class CustomerDto : IMapFrom<Customer>
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Automatic mapping - no Mapping method override needed
}
```

## Using with Application Services

For automatic mapping in the Application layer, inject `IMapper`:

```csharp
public class CustomerApplicationService : ICustomerApplicationService
{
    private readonly IMapper _mapper;
    private readonly IRepositoryAsync<Customer> _repository;

    public CustomerApplicationService(IMapper mapper, IRepositoryAsync<Customer> repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return _mapper.Map<CustomerDto>(entity);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var entities = await _repository.ListAllAsync();
        return _mapper.Map<IEnumerable<CustomerDto>>(entities);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var entity = _mapper.Map<Customer>(request);
        await _repository.AddAsync(entity);
        return _mapper.Map<CustomerDto>(entity);
    }

    public async Task UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var entity = await _repository.GetByIdAsync(id);
        _mapper.Map(request, entity); // Updates existing entity
        await _repository.UpdateAsync(entity);
    }
}
```

### Automatic Registration

All classes implementing `IMapFrom` are automatically registered when calling `AddMvp24HoursMapService`:

```csharp
// Program.cs
builder.Services.AddMvp24HoursMapService(
    Assembly.GetExecutingAssembly(),
    typeof(CustomerDto).Assembly  // Include other assemblies
);
```

---

## See Also

- [AutoMapper Documentation](https://docs.automapper.org/)
- [CQRS with Mapping](cqrs/home.md) - Using mapping with Commands/Queries