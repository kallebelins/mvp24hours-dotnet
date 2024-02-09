# Migração
## Versão v4.2.101
### IBsonClassMap
Remover tipagem genérica de IBsonClassMap<T>:
```csharp
// antes
public class MyEntityConfiguration : IBsonClassMap<MyEntity>

// depois
public class MyEntityConfiguration : IBsonClassMap
```

## Versão v8.2.102
### EntityBase
```csharp
// antes
public class MyEntity : EntityBase<MyEntity, int>

// depois
public class MyEntity : EntityBase<int>
```

### IMapFrom
Remover tipagem genérica de IMapFrom<T>:
```csharp
// antes
public class MyDto : IMapFrom<MyEntity>

// depois
public class MyDto : IMapFrom
```

### TelemetryLevel
Atualizar nome do enumerador TelemetryLevel para plural:
```csharp
// antes
TelemetryHelper.Execute(TelemetryLevel.Verbose, "jwt-test", $"token:xxx");

// depois
TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-test", $"token:xxx");
```

### Mapping
```csharp
// injeção na construção da classe de serviço
private readonly IMapper mapper;
public MyEntityService(IUnitOfWorkAsync unitOfWork, IValidator<MyEntity> validator, IMapper mapper)
	: base(unitOfWork, validator)
{
	this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

#### AutoMapperHelper
```csharp
// antes - anti-pattern singleton
AutoMapperHelper.Map<MyEntity>(entity, dto);

// depois
mapper.Map(dto, entity);
```

#### MapTo
```csharp
// antes
var entity = dto.MapTo<MyEntity>();

// depois
var entity = mapper.Map<MyEntity>(dto);
```

```csharp
// antes
return result.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>();

// depois
mapper.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>(result);
```

### ServiceProviderHelper
```csharp
// antes - anti-pattern singleton
public static IMyEntityService MyEntityService
{
	get { return ServiceProviderHelper.GetService<IMyEntityService>(); }
}

// depois - injeção na construção da classe
private readonly IServiceProvider provider;
public FacadeService(IServiceProvider provider)
{
	this.provider = provider;
}
public IMyEntityService MyEntityService
{
	get { return provider.GetService<IMyEntityService>(); }
}
```

### FacadeService
```csharp
// injeção na construção da classe de controlador
private readonly FacadeService facade;
public MyEntityController(FacadeService facade)
{
	this.facade = facade;
}
```

```csharp
// antes
var result = await FacadeService.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);

// depois
var result = await facade.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);
```

### Statup
Remoção de UseMvp24Hours() da classe de Startup.
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
	// ...
	//app.UseMvp24Hours();
}
```