# Mapping
> A data mapper is a data access layer that performs bidirectional data transfer between a persistent data store (usually a relational database) and an in-memory data representation (the domain layer). [Wikipedia](https://en.wikipedia.org/wiki/Data_mapper_pattern)

## AutoMapper

### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure -Version 9.0.101
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
/// Startup.cs
services.AddMvp24HoursMapService(Assembly.GetExecutingAssembly());
```

### Basic Execution
```csharp
// setting
IMapper mapper = [mapperConfig].CreateMapper();

// running
var classA = new TestAClass { MyProperty1 = 1 };
var classB = mapper.Map<TestIgnoreClass>(classA);
```