# Migration
## Version v8.3.261
### EntityBase
```csharp
// before
public class MyEntity : EntityBase<MyEntity, int>

// after
public class MyEntity : EntityBase<int>
```

### IMapFrom
Remove generic typing from IMapFrom<T>:
```csharp
// before
public class MyDto : IMapFrom<MyEntity>

// after
public class MyDto : IMapFrom
```

### TelemetryLevel
Update TelemetryLevel enumerator name to plural:
```csharp
// before
TelemetryHelper.Execute(TelemetryLevel.Verbose, "jwt-test", $"token:xxx");

// after
TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-test", $"token:xxx");
```

### Mapping
```csharp
// injection into service class construction
private readonly IMapper mapper;
public MyEntityService(IUnitOfWorkAsync unitOfWork, IValidator<MyEntity> validator, IMapper mapper)
	: base(unitOfWork, validator)
{
	this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

#### AutoMapperHelper
```csharp
// before - anti-pattern singleton
AutoMapperHelper.Map<MyEntity>(entity, dto);

// after
mapper.Map(dto, entity);
```

#### MapTo
```csharp
// before
var entity = dto.MapTo<MyEntity>();

// after
var entity = mapper.Map<MyEntity>(dto);
```

```csharp
// before
return result.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>();

// after
mapper.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>(result);
```

### ServiceProviderHelper
```csharp
// before - anti-pattern singleton
public static IMyEntityService MyEntityService
{
	get { return ServiceProviderHelper.GetService<IMyEntityService>(); }
}

// after - injection into class construction
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
// injection in controller class construction
private readonly FacadeService facade;
public MyEntityController(FacadeService facade)
{
	this.facade = facade;
}
```

```csharp
// before
var result = await FacadeService.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);

// after
var result = await facade.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);
```

### Statup
Removed UseMvp24Hours() from the Startup class.
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
	// ...
	//app.UseMvp24Hours();
}
```