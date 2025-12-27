# Smart Enums (Enumeration Pattern)

Smart Enums extend the concept of traditional C# enums by allowing associated behavior, rich domain logic, and better ORM support.

## Why Smart Enums?

Traditional enums have limitations:

```csharp
// Traditional enum - limited
public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

// How to add behavior? Can't do this:
// OrderStatus.Pending.CanCancel(); // Not possible
```

With Smart Enums:

```csharp
// Smart Enum - rich behavior
public class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Processing = new(2, "Processing");
    public static readonly OrderStatus Shipped = new(3, "Shipped");
    
    private OrderStatus(int value, string name) : base(value, name) { }
    
    public virtual bool CanCancel => this == Pending || this == Processing;
}

// Now you can:
if (order.Status.CanCancel)
{
    order.Cancel();
}
```

## Creating Smart Enums

### Basic Structure

```csharp
using Mvp24Hours.Core.Domain.Enumerations;

public class PaymentMethod : Enumeration<PaymentMethod>
{
    public static readonly PaymentMethod CreditCard = new(1, "CreditCard");
    public static readonly PaymentMethod DebitCard = new(2, "DebitCard");
    public static readonly PaymentMethod BankTransfer = new(3, "BankTransfer");
    public static readonly PaymentMethod Pix = new(4, "Pix");
    public static readonly PaymentMethod Cash = new(5, "Cash");
    
    private PaymentMethod(int value, string name) : base(value, name) { }
}
```

### Adding Behavior

```csharp
public class PaymentMethod : Enumeration<PaymentMethod>
{
    public static readonly PaymentMethod CreditCard = new(1, "CreditCard", true, 0.03m);
    public static readonly PaymentMethod DebitCard = new(2, "DebitCard", true, 0.01m);
    public static readonly PaymentMethod BankTransfer = new(3, "BankTransfer", false, 0m);
    public static readonly PaymentMethod Pix = new(4, "Pix", true, 0m);
    public static readonly PaymentMethod Cash = new(5, "Cash", false, 0m);
    
    public bool IsInstant { get; }
    public decimal FeePercentage { get; }
    
    private PaymentMethod(int value, string name, bool isInstant, decimal fee) 
        : base(value, name)
    {
        IsInstant = isInstant;
        FeePercentage = fee;
    }
    
    public decimal CalculateFee(decimal amount) => amount * FeePercentage;
}

// Usage
var method = PaymentMethod.CreditCard;
var fee = method.CalculateFee(100m); // 3.00
Console.WriteLine(method.IsInstant); // true
```

### State Machine Pattern

```csharp
public class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Draft = new(1, "Draft");
    public static readonly OrderStatus Pending = new(2, "Pending");
    public static readonly OrderStatus Confirmed = new(3, "Confirmed");
    public static readonly OrderStatus Processing = new(4, "Processing");
    public static readonly OrderStatus Shipped = new(5, "Shipped");
    public static readonly OrderStatus Delivered = new(6, "Delivered");
    public static readonly OrderStatus Cancelled = new(7, "Cancelled");
    
    private OrderStatus(int value, string name) : base(value, name) { }
    
    public virtual bool CanTransitionTo(OrderStatus newStatus)
    {
        return this switch
        {
            _ when this == Draft => newStatus == Pending || newStatus == Cancelled,
            _ when this == Pending => newStatus == Confirmed || newStatus == Cancelled,
            _ when this == Confirmed => newStatus == Processing || newStatus == Cancelled,
            _ when this == Processing => newStatus == Shipped,
            _ when this == Shipped => newStatus == Delivered,
            _ => false
        };
    }
    
    public virtual bool CanCancel => this == Draft || this == Pending || this == Confirmed;
    public virtual bool IsTerminal => this == Delivered || this == Cancelled;
}

// Usage
var current = OrderStatus.Pending;
var next = OrderStatus.Confirmed;

if (current.CanTransitionTo(next))
{
    order.Status = next;
}
```

## Using Smart Enums

### Lookup by Value

```csharp
// Get by numeric value
var status = OrderStatus.FromValue(1); // Draft

// Safe lookup
if (OrderStatus.TryFromValue(99, out var result))
{
    // Won't reach here - invalid value
}
```

### Lookup by Name

```csharp
// Get by name (case-insensitive)
var status = OrderStatus.FromName("Pending");
var status2 = OrderStatus.FromName("PENDING"); // Same result

// Safe lookup
if (OrderStatus.TryFromName("Unknown", out var result))
{
    // Won't reach here
}
```

### Get All Values

```csharp
// Get all defined values
var allStatuses = OrderStatus.GetAll();

foreach (var status in allStatuses)
{
    Console.WriteLine($"{status.Value}: {status.Name}");
}

// Output:
// 1: Draft
// 2: Pending
// 3: Confirmed
// ...
```

### Check if Defined

```csharp
bool exists = OrderStatus.IsDefined(5);        // true (Shipped)
bool existsByName = OrderStatus.IsDefined("Draft"); // true
```

### Comparisons

```csharp
var pending = OrderStatus.Pending;
var confirmed = OrderStatus.Confirmed;

// Equality
bool same = pending == OrderStatus.Pending; // true
bool different = pending != confirmed;       // true

// Comparison (by Value)
bool less = pending < confirmed; // true (2 < 3)
```

### Implicit Conversions

```csharp
var status = OrderStatus.Pending;

// To int
int value = status; // 2

// To string
string name = status; // "Pending"
```

## Entity Framework Core Integration

### Configuration

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Status)
            .HasConversion(
                v => v.Value,                    // To database (int)
                v => OrderStatus.FromValue(v)   // From database
            );
    }
}
```

### Usage in Queries

```csharp
// Query by value
var pendingOrders = await _context.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .ToListAsync();

// Query by multiple statuses
var activeOrders = await _context.Orders
    .Where(o => o.Status == OrderStatus.Pending 
             || o.Status == OrderStatus.Processing)
    .ToListAsync();
```

## JSON Serialization

```csharp
// System.Text.Json
public class EnumerationJsonConverter<T> : JsonConverter<T> 
    where T : Enumeration<T>
{
    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var value = reader.GetInt32();
        return Enumeration<T>.FromValue(value);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
```

## Real-World Examples

### Document Type

```csharp
public class DocumentType : Enumeration<DocumentType>
{
    public static readonly DocumentType Cpf = new(1, "CPF", 11, @"^\d{11}$");
    public static readonly DocumentType Cnpj = new(2, "CNPJ", 14, @"^\d{14}$");
    public static readonly DocumentType Passport = new(3, "Passport", 9, @"^[A-Z]{2}\d{6}$");
    
    public int Length { get; }
    public string ValidationPattern { get; }
    
    private DocumentType(int value, string name, int length, string pattern) 
        : base(value, name)
    {
        Length = length;
        ValidationPattern = pattern;
    }
    
    public bool Validate(string document)
    {
        if (string.IsNullOrEmpty(document)) return false;
        var clean = Regex.Replace(document, @"[^\dA-Z]", "");
        return Regex.IsMatch(clean, ValidationPattern);
    }
}
```

### Notification Channel

```csharp
public class NotificationChannel : Enumeration<NotificationChannel>
{
    public static readonly NotificationChannel Email = new(1, "Email", true);
    public static readonly NotificationChannel Sms = new(2, "SMS", false);
    public static readonly NotificationChannel Push = new(3, "Push", true);
    public static readonly NotificationChannel WhatsApp = new(4, "WhatsApp", false);
    
    public bool SupportsRichContent { get; }
    
    private NotificationChannel(int value, string name, bool supportsRich) 
        : base(value, name)
    {
        SupportsRichContent = supportsRich;
    }
}
```

## Best Practices

1. **Make constructors private** - Only the static fields should create instances
2. **Use meaningful values** - The `Value` property should be stable for database storage
3. **Keep behavior focused** - Don't overload with unrelated functionality
4. **Consider serialization** - Plan for JSON/database storage from the start
5. **Use for finite, known sets** - Smart Enums work best when all values are known at compile time

