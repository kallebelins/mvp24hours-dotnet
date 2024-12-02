# Mapeamento
> Um mapeador de dados é uma camada de acesso a dados que executa a transferência bidirecional de dados entre um armazenamento de dados persistente (geralmente um banco de dados relacional) e uma representação de dados na memória (a camada de domínio).  [Wikipédia](https://en.wikipedia.org/wiki/Data_mapper_pattern)

## AutoMapper

### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure -Version 9.0.101
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
/// Startup.cs
services.AddMvp24HoursMapService(Assembly.GetExecutingAssembly());
```

### Execução Básica
```csharp
// configurando
IMapper mapper = [mapperConfig].CreateMapper();

// executando
var classA = new TestAClass { MyProperty1 = 1 };
var classB = mapper.Map<TestIgnoreClass>(classA);
```