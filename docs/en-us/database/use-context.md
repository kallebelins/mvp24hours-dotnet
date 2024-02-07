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
Your entity must implement a log interface. [See Entity](pt-br/database/use-entity.md)

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
