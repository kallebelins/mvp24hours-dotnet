# Guard Clauses

Guard clauses provide a fluent API for defensive programming, validating method arguments and throwing appropriate exceptions when validations fail.

## Overview

The `Guard` class provides a static entry point `Guard.Against` that offers various validation methods:

```csharp
using Mvp24Hours.Core.Helpers;

public void ProcessOrder(Order order, string customerId, decimal amount)
{
    Guard.Against.Null(order, nameof(order));
    Guard.Against.NullOrEmpty(customerId, nameof(customerId));
    Guard.Against.NegativeOrZero(amount, nameof(amount));
    
    // Your logic here - parameters are guaranteed to be valid
}
```

## Available Guard Methods

### Null Checks

#### Guard.Against.Null&lt;T&gt;

Throws `ArgumentNullException` if the value is null.

```csharp
public void SendEmail(EmailMessage message)
{
    Guard.Against.Null(message, nameof(message));
    // message is guaranteed to be non-null here
}
```

#### Guard.Against.NullOrEmpty (string)

Throws `ArgumentNullException` if null, `ArgumentException` if empty.

```csharp
public User CreateUser(string username, string email)
{
    Guard.Against.NullOrEmpty(username, nameof(username));
    Guard.Against.NullOrEmpty(email, nameof(email));
    
    return new User(username, email);
}
```

#### Guard.Against.NullOrWhiteSpace

Throws exception if null, empty, or whitespace only.

```csharp
public void SetDescription(string description)
{
    Guard.Against.NullOrWhiteSpace(description, nameof(description));
}
```

#### Guard.Against.NullOrEmpty&lt;T&gt; (collections)

Throws exception if collection is null or empty.

```csharp
public decimal CalculateAverage(IEnumerable<decimal> values)
{
    Guard.Against.NullOrEmpty(values, nameof(values));
    return values.Average();
}
```

### Default Value Checks

#### Guard.Against.Default&lt;T&gt;

Throws `ArgumentException` if value equals `default(T)`. Useful for structs like `Guid.Empty`.

```csharp
public Order GetOrder(Guid orderId)
{
    Guard.Against.Default(orderId, nameof(orderId));
    // orderId is guaranteed to not be Guid.Empty
    
    return _repository.GetById(orderId);
}
```

#### Guard.Against.EmptyGuid

Specifically checks for `Guid.Empty`.

```csharp
public void AssignToUser(Guid userId)
{
    Guard.Against.EmptyGuid(userId, nameof(userId));
}
```

### Range Checks

#### Guard.Against.OutOfRange&lt;T&gt;

Throws `ArgumentOutOfRangeException` if value is outside the specified range.

```csharp
public void SetRating(int rating)
{
    Guard.Against.OutOfRange(rating, 1, 5, nameof(rating));
    // rating is guaranteed to be between 1 and 5
}

public void SetDiscount(decimal percentage)
{
    Guard.Against.OutOfRange(percentage, 0m, 100m, nameof(percentage));
}
```

#### Guard.Against.NegativeOrZero

Throws exception if value is zero or negative.

```csharp
public void SetQuantity(int quantity)
{
    Guard.Against.NegativeOrZero(quantity, nameof(quantity));
}

public void SetPrice(decimal price)
{
    Guard.Against.NegativeOrZero(price, nameof(price));
}
```

#### Guard.Against.Negative

Throws exception if value is negative (zero is allowed).

```csharp
public void SetBalance(decimal balance)
{
    Guard.Against.Negative(balance, nameof(balance));
    // balance can be 0 or positive
}
```

#### Guard.Against.LessThan / GreaterThan

```csharp
public void SetAge(int age)
{
    Guard.Against.LessThan(age, 0, nameof(age));
    Guard.Against.GreaterThan(age, 150, nameof(age));
}
```

### String Length Checks

#### Guard.Against.LengthLessThan / LengthGreaterThan

```csharp
public void SetPassword(string password)
{
    Guard.Against.NullOrEmpty(password, nameof(password));
    Guard.Against.LengthLessThan(password, 8, nameof(password));
    Guard.Against.LengthGreaterThan(password, 100, nameof(password));
}
```

#### Guard.Against.LengthOutOfRange

```csharp
public void SetUsername(string username)
{
    Guard.Against.NullOrEmpty(username, nameof(username));
    Guard.Against.LengthOutOfRange(username, 3, 50, nameof(username));
}
```

### Format Validations

#### Guard.Against.InvalidFormat

Validates against a regex pattern.

```csharp
public void SetProductCode(string code)
{
    Guard.Against.InvalidFormat(code, @"^[A-Z]{2}-\d{4}$", nameof(code));
    // code must match pattern like "AB-1234"
}
```

#### Guard.Against.InvalidEmail

Validates email format (RFC 5322 compliant).

```csharp
public void SetEmail(string email)
{
    Guard.Against.InvalidEmail(email, nameof(email));
}
```

### Brazilian Document Validations

#### Guard.Against.InvalidCpf

Validates Brazilian CPF (individual taxpayer registry).

```csharp
public void SetCpf(string cpf)
{
    Guard.Against.InvalidCpf(cpf, nameof(cpf));
    // Validates format and check digits
    // Accepts: "123.456.789-09" or "12345678909"
}
```

#### Guard.Against.InvalidCnpj

Validates Brazilian CNPJ (company taxpayer registry).

```csharp
public void SetCnpj(string cnpj)
{
    Guard.Against.InvalidCnpj(cnpj, nameof(cnpj));
    // Validates format and check digits
    // Accepts: "12.345.678/0001-95" or "12345678000195"
}
```

### Type Checks

#### Guard.Against.NotOfType&lt;T&gt;

Validates that an object is of a specific type.

```csharp
public void ProcessPayment(IPaymentMethod method)
{
    var creditCard = Guard.Against.NotOfType<CreditCardPayment>(
        method, 
        nameof(method)
    );
    // creditCard is now typed as CreditCardPayment
}
```

### Condition Checks

#### Guard.Against.Condition

Throws `ArgumentException` if condition is true.

```csharp
public void Transfer(Account from, Account to, decimal amount)
{
    Guard.Against.Condition(
        from.Id == to.Id,
        nameof(to),
        "Cannot transfer to the same account"
    );
}
```

#### Guard.Against.InvalidOperation

Throws `InvalidOperationException` if condition is true. Use for state validation.

```csharp
public void Ship(Order order)
{
    Guard.Against.InvalidOperation(
        order.Status != OrderStatus.Paid,
        "Cannot ship an unpaid order"
    );
}
```

## Custom Error Messages

All guard methods accept an optional custom message:

```csharp
Guard.Against.Null(
    customer, 
    nameof(customer), 
    "Customer must be provided to create an order"
);

Guard.Against.NegativeOrZero(
    amount, 
    nameof(amount),
    "Order amount must be greater than zero"
);
```

## Fluent Returns

Guard methods return the validated value, enabling fluent usage:

```csharp
public class Order
{
    public string CustomerId { get; }
    public decimal Amount { get; }
    
    public Order(string customerId, decimal amount)
    {
        CustomerId = Guard.Against.NullOrEmpty(customerId, nameof(customerId));
        Amount = Guard.Against.NegativeOrZero(amount, nameof(amount));
    }
}
```

## Best Practices

### 1. Guard at Entry Points

Place guards at the beginning of public methods:

```csharp
public async Task<Order> CreateOrderAsync(
    string customerId, 
    IEnumerable<OrderItem> items,
    Address shippingAddress)
{
    // All guards first
    Guard.Against.NullOrEmpty(customerId, nameof(customerId));
    Guard.Against.NullOrEmpty(items, nameof(items));
    Guard.Against.Null(shippingAddress, nameof(shippingAddress));
    
    // Then business logic
    var customer = await _customerService.GetAsync(customerId);
    // ...
}
```

### 2. Use Specific Guards

Choose the most specific guard for your validation:

```csharp
// Good - specific
Guard.Against.EmptyGuid(orderId, nameof(orderId));

// Less good - generic
Guard.Against.Default(orderId, nameof(orderId));
```

### 3. Combine with Value Objects

Use guards in Value Object constructors:

```csharp
public class Email
{
    public string Value { get; }
    
    public Email(string value)
    {
        Value = Guard.Against.InvalidEmail(value, nameof(value));
    }
}
```

### 4. Domain Validation vs Guard Clauses

- **Guard clauses**: For argument validation (programming errors)
- **Domain validation**: For business rules (user input errors)

```csharp
// Guard clause - programming error if null
Guard.Against.Null(order, nameof(order));

// Domain validation - user input error
if (order.Total > customer.CreditLimit)
{
    throw new BusinessException("Order exceeds credit limit");
}
```

