# How to implement database context?
Context represents a session with the database and can be used to query and save entity instances.

## Basic Configuration
```csharp
public class MyDataContext : Mvp24HoursContext
{
    public MyDataContext()
        : base()
    {
    }

    public MyDataContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

## Configuration with Log
If you want to control entity logs dynamically, simply apply the configuration below:
```csharp
public class MyDataContext : Mvp24HoursContext
{
    [...]
    public override bool CanApplyEntityLog => true;
}
```
Your entity must implement a log interface. [See Entity](use-entity.md)

One of the logging implementations offers the possibility to fill in the ID of the user who is creating, updating or deleting the record (logical deletion). To load logged in user data, I suggest:
```csharp
public class MyDataContext : Mvp24HoursContext
{
    private readonly IHttpContextAccessor accessor;

    public MyDataContext(IHttpContextAccessor accessor)
        : base()
    {
        this.accessor = accessor;
    }

    public MyDataContext(DbContextOptions options, IHttpContextAccessor accessor)
        : base(options)
    {
        this.accessor = accessor;
    }

    public override object EntityLogBy => this.accessor.MyExtensionGetUser();

    public override bool CanApplyEntityLog => true;

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

## Modern Approach with ICurrentUserProvider and IClock

For .NET 9+ applications, use the `ICurrentUserProvider` and `IClock` (or `TimeProvider`) interfaces for better testability:

```csharp
public class MyDataContext : Mvp24HoursContext
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IClock _clock; // or TimeProvider for .NET 9+

    public MyDataContext(
        DbContextOptions options, 
        ICurrentUserProvider currentUserProvider,
        IClock clock)
        : base(options)
    {
        _currentUserProvider = currentUserProvider;
        _clock = clock;
    }

    public override object EntityLogBy => _currentUserProvider.GetUserId();
    
    public override DateTime GetCurrentDateTime() => _clock.UtcNow.DateTime;

    public override bool CanApplyEntityLog => true;

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

Register the providers in your `Program.cs`:

```csharp
// Program.cs
builder.Services.AddScoped<ICurrentUserProvider, HttpContextUserProvider>();
builder.Services.AddSingleton<IClock, SystemClock>();
// Or for .NET 9+:
builder.Services.AddSingleton(TimeProvider.System);
```
