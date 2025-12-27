# Strongly-Typed IDs

Strongly-typed IDs prevent accidental mixing of identifiers from different entity types, providing compile-time safety.

## The Problem

With primitive IDs, nothing prevents mixing them up:

```csharp
// Dangerous - both are Guid
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
}

public void ProcessOrder(Guid orderId, Guid customerId)
{
    // Easy to swap by mistake - compiles fine!
    var order = _repo.GetOrder(customerId); // Bug!
}
```

## The Solution

With strongly-typed IDs, the compiler catches mistakes:

```csharp
public class Order
{
    public OrderId Id { get; set; }
    public CustomerId CustomerId { get; set; }
}

public void ProcessOrder(OrderId orderId, CustomerId customerId)
{
    // This won't compile - type mismatch!
    // var order = _repo.GetOrder(customerId);
    
    var order = _repo.GetOrder(orderId); // Correct
}
```

## Creating Strongly-Typed IDs

### Using GuidEntityId

```csharp
using Mvp24Hours.Core.ValueObjects;

public sealed class CustomerId : GuidEntityId<CustomerId>
{
    public CustomerId(Guid value) : base(value) { }
    
    public static CustomerId New() => new(Guid.NewGuid());
    public static CustomerId Empty => new(Guid.Empty);
}

public sealed class OrderId : GuidEntityId<OrderId>
{
    public OrderId(Guid value) : base(value) { }
    
    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId Empty => new(Guid.Empty);
}

public sealed class ProductId : GuidEntityId<ProductId>
{
    public ProductId(Guid value) : base(value) { }
    
    public static ProductId New() => new(Guid.NewGuid());
}
```

### Using IntEntityId

```csharp
public sealed class CategoryId : IntEntityId<CategoryId>
{
    public CategoryId(int value) : base(value) { }
}

public sealed class SequenceNumber : IntEntityId<SequenceNumber>
{
    public SequenceNumber(int value) : base(value) { }
}
```

### Using LongEntityId

```csharp
public sealed class TransactionId : LongEntityId<TransactionId>
{
    public TransactionId(long value) : base(value) { }
}
```

### Using StringEntityId

```csharp
public sealed class Sku : StringEntityId<Sku>
{
    public Sku(string value) : base(value) { }
    
    public static Sku Create(string value)
    {
        Guard.Against.NullOrEmpty(value, nameof(value));
        Guard.Against.InvalidFormat(value, @"^[A-Z]{3}-\d{6}$", nameof(value));
        return new Sku(value);
    }
}
```

## Usage in Entities

```csharp
public class Customer
{
    public CustomerId Id { get; private set; }
    public string Name { get; private set; }
    
    public Customer(string name)
    {
        Id = CustomerId.New();
        Name = name;
    }
}

public class Order
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public List<OrderItem> Items { get; private set; }
    
    public Order(CustomerId customerId)
    {
        Id = OrderId.New();
        CustomerId = customerId;
        Items = new List<OrderItem>();
    }
}

public class OrderItem
{
    public OrderItemId Id { get; private set; }
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
}
```

## Features

### Equality

```csharp
var id1 = CustomerId.New();
var id2 = new CustomerId(id1.Value);

bool areEqual = id1 == id2;        // true
bool areEqual2 = id1.Equals(id2);  // true
```

### Comparison

```csharp
var ids = new List<CustomerId> { id3, id1, id2 };
ids.Sort(); // Works - implements IComparable
```

### Empty/Default Checks

```csharp
var customerId = CustomerId.New();
var emptyId = CustomerId.Empty;

bool isEmpty = emptyId.IsEmpty;   // true (GuidEntityId)
bool isDefault = categoryId.IsDefault; // true for 0 (IntEntityId)
```

### Implicit Conversion to Underlying Type

```csharp
var customerId = CustomerId.New();

// Implicit conversion to Guid
Guid guidValue = customerId;

// Use in LINQ
var orders = _context.Orders
    .Where(o => o.CustomerId.Value == customerId)
    .ToList();
```

## Entity Framework Core Integration

### Value Converter

```csharp
public class CustomerIdConverter : ValueConverter<CustomerId, Guid>
{
    public CustomerIdConverter()
        : base(
            id => id.Value,
            guid => new CustomerId(guid))
    { }
}

// In DbContext OnModelCreating
modelBuilder.Entity<Customer>()
    .Property(c => c.Id)
    .HasConversion<CustomerIdConverter>();

modelBuilder.Entity<Order>()
    .Property(o => o.CustomerId)
    .HasConversion<CustomerIdConverter>();
```

### Generic Converter

```csharp
// For all GuidEntityId types
public class GuidEntityIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : GuidEntityId<TId>
{
    public GuidEntityIdConverter()
        : base(
            id => id.Value,
            guid => (TId)Activator.CreateInstance(typeof(TId), guid)!)
    { }
}
```

### Configuration Extension

```csharp
public static class ModelBuilderExtensions
{
    public static void UseStronglyTypedIds(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsSubclassOfGeneric(typeof(GuidEntityId<>)))
                {
                    var converterType = typeof(GuidEntityIdConverter<>)
                        .MakeGenericType(property.ClrType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                }
            }
        }
    }
}
```

## JSON Serialization

### System.Text.Json

```csharp
public class GuidEntityIdJsonConverter<TId> : JsonConverter<TId>
    where TId : GuidEntityId<TId>
{
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var guid = reader.GetGuid();
        return (TId)Activator.CreateInstance(typeof(TId), guid)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
```

### Newtonsoft.Json

The `EntityIdNewtonsoftConverters` class is provided in `Mvp24Hours.Core.Converters`:

```csharp
var settings = new JsonSerializerSettings
{
    Converters = { new GuidEntityIdNewtonsoftConverter<CustomerId>() }
};
```

## Repository Pattern

```csharp
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id);
    Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<CustomerId> ids);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly DbContext _context;
    
    public async Task<Customer?> GetByIdAsync(CustomerId id)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<CustomerId> ids)
    {
        var guidIds = ids.Select(id => id.Value).ToList();
        return await _context.Customers
            .Where(c => guidIds.Contains(c.Id.Value))
            .ToListAsync();
    }
}
```

## API Controllers

```csharp
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> Get(Guid id)
    {
        var customerId = new CustomerId(id);
        var customer = await _repository.GetByIdAsync(customerId);
        
        return customer is null ? NotFound() : Ok(customer);
    }
    
    [HttpGet("{customerId}/orders")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders(Guid customerId)
    {
        var id = new CustomerId(customerId);
        var orders = await _orderRepository.GetByCustomerIdAsync(id);
        return Ok(orders);
    }
}
```

## Best Practices

1. **Create one ID type per entity** - `CustomerId`, `OrderId`, `ProductId`
2. **Use the appropriate base class** - `GuidEntityId`, `IntEntityId`, `LongEntityId`, or `StringEntityId`
3. **Add factory methods** - `New()`, `Empty`, `Create()` for convenience
4. **Configure EF Core converters** - Ensure proper database mapping
5. **Handle serialization** - Configure JSON converters for APIs

