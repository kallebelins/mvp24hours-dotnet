# Functional Patterns

The Mvp24Hours.Core module provides functional programming patterns for safer, more expressive code.

## Maybe&lt;T&gt;

`Maybe<T>` (also known as `Option`) represents a value that may or may not exist, providing a type-safe alternative to null.

### Why Use Maybe&lt;T&gt;?

```csharp
// Without Maybe - null reference danger
Customer customer = repository.GetById(id);
string name = customer.Name; // NullReferenceException if customer is null!

// With Maybe - forced to handle absence
Maybe<Customer> customer = repository.GetById(id);
string name = customer
    .Map(c => c.Name)
    .ValueOr("Unknown");
```

### Creating Maybe Values

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

// Create a Maybe with a value
var some = Maybe<int>.Some(42);
var some2 = Maybe.Some(42); // Using static helper

// Create an empty Maybe
var none = Maybe<int>.None;
var none2 = Maybe.None<int>(); // Using static helper

// Create from a potentially null value
string name = GetName(); // might be null
var maybeName = Maybe.From(name); // Some if not null, None if null

// Implicit conversion
Maybe<int> value = 42; // Automatically becomes Some(42)
Maybe<string> empty = null; // Automatically becomes None
```

### Checking for Value

```csharp
var maybe = Maybe.Some(42);

if (maybe.HasValue)
{
    Console.WriteLine($"Value: {maybe.Value}");
}

if (maybe.HasNoValue)
{
    Console.WriteLine("No value present");
}
```

### Getting the Value Safely

#### ValueOr - Default Value

```csharp
var maybe = Maybe<int>.None;

// With literal default
int value = maybe.ValueOr(0);

// With factory function (lazy evaluation)
int value = maybe.ValueOr(() => CalculateDefault());
```

#### Match - Pattern Matching

```csharp
var maybeCustomer = FindCustomer("123");

// Return a value based on presence
string message = maybeCustomer.Match(
    some: customer => $"Hello, {customer.Name}!",
    none: () => "Customer not found"
);

// Execute actions based on presence
maybeCustomer.Match(
    some: customer => SendWelcomeEmail(customer),
    none: () => LogMissingCustomer()
);
```

### Transforming Values

#### Map - Transform the Value

```csharp
var maybeCustomer = FindCustomer("123");

// Transform Customer to string
Maybe<string> maybeName = maybeCustomer.Map(c => c.Name);

// Chain multiple maps
Maybe<string> maybeUpperName = maybeCustomer
    .Map(c => c.Name)
    .Map(name => name.ToUpper());
```

#### Bind - Chain Maybe-Returning Functions

```csharp
Maybe<Customer> FindCustomer(string id) { /* ... */ }
Maybe<Order> GetLatestOrder(Customer customer) { /* ... */ }
Maybe<Product> GetFirstProduct(Order order) { /* ... */ }

// Chain operations that return Maybe
Maybe<Product> product = FindCustomer("123")
    .Bind(customer => GetLatestOrder(customer))
    .Bind(order => GetFirstProduct(order));
```

### Filtering

#### Where - Conditional Filtering

```csharp
var maybeNumber = Maybe.Some(42);

// Returns None if predicate is false
var maybeEven = maybeNumber.Where(n => n % 2 == 0); // Some(42)
var maybeLarge = maybeNumber.Where(n => n > 100);   // None
```

### Side Effects

#### Tap - Execute Action Without Changing Value

```csharp
FindCustomer("123")
    .Tap(customer => Log($"Found customer: {customer.Id}"))
    .Map(customer => customer.Email);
```

### Converting

```csharp
var maybe = Maybe.Some("hello");

// Convert to nullable
string value = maybe.ToNullable(); // "hello" or null
```

### Real-World Examples

#### Repository Pattern

```csharp
public interface ICustomerRepository
{
    Maybe<Customer> GetById(Guid id);
    Maybe<Customer> GetByEmail(string email);
}

public class CustomerService
{
    private readonly ICustomerRepository _repository;
    
    public string GetCustomerGreeting(Guid id)
    {
        return _repository.GetById(id)
            .Map(c => $"Hello, {c.Name}!")
            .ValueOr("Hello, Guest!");
    }
    
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        return _repository.GetById(id)
            .Match<IActionResult>(
                some: customer => Ok(customer),
                none: () => NotFound()
            );
    }
}
```

#### Chaining Operations

```csharp
public Maybe<decimal> CalculateDiscount(string customerId, string productId)
{
    return _customerRepository.GetById(customerId)
        .Bind(customer => GetLoyaltyTier(customer))
        .Bind(tier => _productRepository.GetById(productId)
            .Map(product => CalculateDiscountAmount(tier, product)));
}
```

#### Configuration Lookup

```csharp
public class ConfigService
{
    private readonly Dictionary<string, string> _config;
    
    public Maybe<string> GetValue(string key)
    {
        return _config.TryGetValue(key, out var value)
            ? Maybe.Some(value)
            : Maybe<string>.None;
    }
    
    public Maybe<int> GetIntValue(string key)
    {
        return GetValue(key)
            .Bind(value => int.TryParse(value, out var number)
                ? Maybe.Some(number)
                : Maybe<int>.None);
    }
}
```

---

## Either&lt;TLeft, TRight&gt;

`Either<TLeft, TRight>` represents a value that is one of two possible types - typically used for operations that can fail, where `Left` represents failure and `Right` represents success.

### Creating Either Values

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

// Success (Right)
Either<string, int> success = Either<string, int>.Right(42);

// Failure (Left)
Either<string, int> failure = Either<string, int>.Left("Error occurred");
```

### Pattern Matching

```csharp
Either<string, Customer> result = CreateCustomer(data);

// Match to get a value
string message = result.Match(
    left: error => $"Failed: {error}",
    right: customer => $"Created: {customer.Name}"
);

// Match to execute actions
result.Match(
    left: error => LogError(error),
    right: customer => NotifyCreation(customer)
);
```

### Transforming

```csharp
Either<string, int> result = ParseNumber(input);

// Map the right (success) value
Either<string, string> formatted = result.MapRight(n => $"Number: {n}");

// Map the left (error) value
Either<ErrorDetails, int> detailed = result.MapLeft(msg => new ErrorDetails(msg));
```

### Real-World Example

```csharp
public Either<ValidationError, Customer> CreateCustomer(CreateCustomerDto dto)
{
    if (string.IsNullOrEmpty(dto.Email))
    {
        return Either<ValidationError, Customer>.Left(
            new ValidationError("Email is required")
        );
    }
    
    if (!IsValidEmail(dto.Email))
    {
        return Either<ValidationError, Customer>.Left(
            new ValidationError("Invalid email format")
        );
    }
    
    var customer = new Customer(dto.Name, dto.Email);
    return Either<ValidationError, Customer>.Right(customer);
}

// Usage
var result = CreateCustomer(dto);
return result.Match(
    left: error => BadRequest(error),
    right: customer => Ok(customer)
);
```

---

## Maybe Extensions

The `MaybeExtensions` class provides additional utility methods.

### Converting from BusinessResult

```csharp
using Mvp24Hours.Core.Extensions.Functional;

IBusinessResult<Customer> result = await _service.GetCustomerAsync(id);

// Convert to Maybe (None if result has errors or null data)
Maybe<Customer> maybe = result.ToMaybe();
```

### Working with Collections

```csharp
// Get first element as Maybe
var items = new List<int> { 1, 2, 3 };
Maybe<int> first = items.FirstOrNone();          // Some(1)
Maybe<int> found = items.FirstOrNone(x => x > 5); // None

// Get single element as Maybe
Maybe<int> single = items.SingleOrNone(x => x == 2); // Some(2)
```

---

## Best Practices

### 1. Use Maybe for Optional Return Values

```csharp
// Good - explicit about possible absence
public Maybe<Customer> FindByEmail(string email);

// Avoid - null is implicit
public Customer FindByEmail(string email);
```

### 2. Use Either for Operations That Can Fail

```csharp
// Good - error type is explicit
public Either<ValidationError, Order> CreateOrder(OrderDto dto);

// Alternative - use Result pattern
public IBusinessResult<Order> CreateOrder(OrderDto dto);
```

### 3. Prefer Map/Bind Over Manual Checks

```csharp
// Good - functional style
var name = maybeCustomer
    .Map(c => c.Name)
    .ValueOr("Unknown");

// Avoid - imperative style
string name;
if (maybeCustomer.HasValue)
    name = maybeCustomer.Value.Name;
else
    name = "Unknown";
```

### 4. Don't Use Maybe for Required Values

```csharp
// Bad - if customer is always required, use Guard
public void ProcessOrder(Maybe<Customer> customer)

// Good - validate at entry point
public void ProcessOrder(Customer customer)
{
    Guard.Against.Null(customer, nameof(customer));
}
```

