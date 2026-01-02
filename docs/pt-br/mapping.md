# Mapeamento
> Um mapeador de dados é uma camada de acesso a dados que executa a transferência bidirecional de dados entre um armazenamento de dados persistente (geralmente um banco de dados relacional) e uma representação de dados na memória (a camada de domínio).  [Wikipédia](https://en.wikipedia.org/wiki/Data_mapper_pattern)

## AutoMapper

### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure -Version 9.1.x
```

### Configuração Básica
Implementar o contrato Mapping da interface IMapFrom.

```csharp
/// CustomerResponse.cs
public class CustomerResponse : IMapFrom
{
    public string Name { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<Customer, CustomerResponse>();
}
```

#### Ignorando Propriedades
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

#### Mapeando Novas Propriedades
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

### Carregando Configurações
```csharp
/// Program.cs
builder.Services.AddMvp24HoursMapService(Assembly.GetExecutingAssembly());
```

### Execução Básica
```csharp
// configurando
IMapper mapper = [mapperConfig].CreateMapper();

// executando
var classA = new TestAClass { MyProperty1 = 1 };
var classB = mapper.Map<TestIgnoreClass>(classA);
```

## Interface IMapFrom

A interface `IMapFrom` é a forma recomendada de definir mapeamentos na arquitetura Mvp24Hours. Ela permite definir mapeamentos diretamente nos seus DTOs/ViewModels, mantendo os mapeamentos próximos às classes que afetam.

```csharp
/// <summary>
/// Interface para configurar mapeamentos do AutoMapper
/// </summary>
public interface IMapFrom
{
    /// <summary>
    /// Define a configuração do mapeamento
    /// </summary>
    void Mapping(Profile profile);
}
```

### Alternativa: IMapFrom<T>

Para mapeamentos mais simples sem configuração customizada:

```csharp
public interface IMapFrom<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
}

// Uso
public class CustomerDto : IMapFrom<Customer>
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Mapeamento automático - não precisa sobrescrever o método Mapping
}
```

## Usando com Application Services

Para mapeamento automático na camada Application, injete `IMapper`:

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
        _mapper.Map(request, entity); // Atualiza entidade existente
        await _repository.UpdateAsync(entity);
    }
}
```

### Registro Automático

Todas as classes que implementam `IMapFrom` são automaticamente registradas ao chamar `AddMvp24HoursMapService`:

```csharp
// Program.cs
builder.Services.AddMvp24HoursMapService(
    Assembly.GetExecutingAssembly(),
    typeof(CustomerDto).Assembly  // Incluir outros assemblies
);
```

---

## Consulte Também

- [Documentação do AutoMapper](https://docs.automapper.org/)
- [CQRS com Mapeamento](cqrs/home.md) - Usando mapeamento com Commands/Queries