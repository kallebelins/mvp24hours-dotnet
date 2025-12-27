# Infrastructure Abstractions

The Mvp24Hours.Core module provides abstractions for infrastructure concerns, enabling testability and decoupling from concrete implementations.

## IClock

Abstracts system time for testability and time manipulation.

### Why Use IClock?

```csharp
// Hard to test - depends on real time
public bool IsExpired(DateTime expirationDate)
{
    return DateTime.UtcNow > expirationDate; // Can't control in tests
}

// Testable - uses abstraction
public bool IsExpired(DateTime expirationDate, IClock clock)
{
    return clock.UtcNow > expirationDate; // Can inject test clock
}
```

### Interface

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTime UtcToday { get; }
    DateTime Today { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }
}
```

### SystemClock (Production)

```csharp
// Registration
services.AddSingleton<IClock, SystemClock>();

// Usage
public class OrderService
{
    private readonly IClock _clock;
    
    public OrderService(IClock clock)
    {
        _clock = clock;
    }
    
    public Order CreateOrder(Cart cart)
    {
        return new Order
        {
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(30)
        };
    }
}
```

### TestClock (Testing)

```csharp
public class TestClock : IClock
{
    private DateTime _currentTime;
    
    public TestClock(DateTime startTime)
    {
        _currentTime = startTime;
    }
    
    public DateTime UtcNow => _currentTime;
    public DateTime Now => _currentTime.ToLocalTime();
    public DateTime UtcToday => _currentTime.Date;
    public DateTime Today => _currentTime.ToLocalTime().Date;
    public DateTimeOffset UtcNowOffset => new(_currentTime, TimeSpan.Zero);
    public DateTimeOffset NowOffset => new(_currentTime.ToLocalTime(), DateTimeOffset.Now.Offset);
    
    public void AdvanceBy(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
    
    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }
}
```

### Test Example

```csharp
[Fact]
public void Order_Should_Expire_After_30_Days()
{
    // Arrange
    var startDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    var clock = new TestClock(startDate);
    var service = new OrderService(clock);
    
    var order = service.CreateOrder(cart);
    
    // Act - advance time by 31 days
    clock.AdvanceBy(TimeSpan.FromDays(31));
    
    // Assert
    Assert.True(service.IsOrderExpired(order));
}

[Fact]
public void Order_Should_Not_Expire_Within_30_Days()
{
    // Arrange
    var startDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    var clock = new TestClock(startDate);
    var service = new OrderService(clock);
    
    var order = service.CreateOrder(cart);
    
    // Act - advance time by 29 days
    clock.AdvanceBy(TimeSpan.FromDays(29));
    
    // Assert
    Assert.False(service.IsOrderExpired(order));
}
```

---

## IGuidGenerator

Abstracts GUID generation for testability and special generation strategies.

### Interface

```csharp
public interface IGuidGenerator
{
    Guid NewGuid();
}
```

### DefaultGuidGenerator

Standard GUID generation:

```csharp
public class DefaultGuidGenerator : IGuidGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}

// Registration
services.AddSingleton<IGuidGenerator, DefaultGuidGenerator>();
```

### SequentialGuidGenerator

Generates sequential GUIDs optimized for database indexes (SQL Server friendly):

```csharp
public class SequentialGuidGenerator : IGuidGenerator
{
    public Guid NewGuid()
    {
        // Creates GUIDs that are sequential when sorted
        // Better index performance in SQL Server
        return CreateSequentialGuid();
    }
}

// Registration for SQL Server
services.AddSingleton<IGuidGenerator, SequentialGuidGenerator>();
```

### DeterministicGuidGenerator (Testing)

Generates predictable GUIDs for testing:

```csharp
public class DeterministicGuidGenerator : IGuidGenerator
{
    private int _counter;
    
    public Guid NewGuid()
    {
        _counter++;
        return new Guid($"00000000-0000-0000-0000-{_counter:D12}");
    }
    
    public void Reset()
    {
        _counter = 0;
    }
}

// Usage in tests
[Fact]
public void Should_Create_Customer_With_Id()
{
    // Arrange
    var guidGenerator = new DeterministicGuidGenerator();
    var service = new CustomerService(guidGenerator);
    
    // Act
    var customer1 = service.CreateCustomer("John");
    var customer2 = service.CreateCustomer("Jane");
    
    // Assert - predictable IDs
    Assert.Equal(new Guid("00000000-0000-0000-0000-000000000001"), customer1.Id);
    Assert.Equal(new Guid("00000000-0000-0000-0000-000000000002"), customer2.Id);
}
```

### Usage in Services

```csharp
public class CustomerService
{
    private readonly IGuidGenerator _guidGenerator;
    
    public CustomerService(IGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }
    
    public Customer CreateCustomer(string name)
    {
        return new Customer
        {
            Id = _guidGenerator.NewGuid(),
            Name = name
        };
    }
}
```

---

## ICurrentUserProvider

Abstracts access to the current user context.

```csharp
public interface ICurrentUserProvider
{
    string UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

### Implementation for ASP.NET Core

```csharp
public class HttpContextUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public HttpContextUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? "anonymous";
    
    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name 
        ?? "Anonymous";
    
    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) 
        ?? Enumerable.Empty<string>();
    
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
```

### Test Implementation

```csharp
public class TestUserProvider : ICurrentUserProvider
{
    public string UserId { get; set; } = "test-user-id";
    public string UserName { get; set; } = "Test User";
    public IEnumerable<string> Roles { get; set; } = new[] { "User" };
    public bool IsAuthenticated { get; set; } = true;
}
```

---

## ITenantProvider

Abstracts multi-tenant context resolution.

```csharp
public interface ITenantProvider
{
    string TenantId { get; }
    string TenantName { get; }
}
```

### Implementation

```csharp
public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public string TenantId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
        ?? throw new InvalidOperationException("Tenant header not found");
    
    public string TenantName =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Name"].FirstOrDefault()
        ?? TenantId;
}
```

---

## Registration

```csharp
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureAbstractions(
        this IServiceCollection services)
    {
        // Clock
        services.AddSingleton<IClock, SystemClock>();
        
        // GUID Generator (use SequentialGuidGenerator for SQL Server)
        services.AddSingleton<IGuidGenerator, DefaultGuidGenerator>();
        
        // User Provider
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, HttpContextUserProvider>();
        
        return services;
    }
    
    public static IServiceCollection AddTestInfrastructure(
        this IServiceCollection services,
        DateTime? startTime = null)
    {
        var clock = new TestClock(startTime ?? DateTime.UtcNow);
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(clock); // Also as concrete type for test control
        
        var guidGen = new DeterministicGuidGenerator();
        services.AddSingleton<IGuidGenerator>(guidGen);
        services.AddSingleton(guidGen);
        
        services.AddSingleton<ICurrentUserProvider>(new TestUserProvider());
        
        return services;
    }
}
```

---

## Best Practices

1. **Always inject IClock** - Never use `DateTime.Now` or `DateTime.UtcNow` directly
2. **Use IGuidGenerator for entity IDs** - Enables predictable testing
3. **Consider SequentialGuidGenerator** - Better database performance for SQL Server
4. **Create test implementations** - Make testing deterministic and fast
5. **Register as appropriate lifetime** - Singletons for stateless, Scoped for request-specific

