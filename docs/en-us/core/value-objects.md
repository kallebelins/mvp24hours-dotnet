# Value Objects

Value Objects are immutable objects that represent domain concepts through their attributes rather than identity. The Mvp24Hours.Core module provides ready-to-use Value Objects for common scenarios.

## Available Value Objects

| Value Object | Purpose |
|-------------|---------|
| `Email` | Email addresses with validation |
| `Cpf` | Brazilian individual taxpayer ID |
| `Cnpj` | Brazilian company taxpayer ID |
| `Money` | Monetary values with currency |
| `Address` | Physical addresses |
| `DateRange` | Date/time ranges |
| `Percentage` | Percentage values |
| `PhoneNumber` | International phone numbers |

---

## Email

Represents a validated email address.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create with validation
var email = Email.Create("user@example.com");

// Properties
Console.WriteLine(email.Value);     // user@example.com
Console.WriteLine(email.LocalPart); // user
Console.WriteLine(email.Domain);    // example.com

// Safe parsing
if (Email.TryParse("user@example.com", out var result))
{
    Console.WriteLine($"Valid: {result.Value}");
}

// Validation only
bool isValid = Email.IsValid("user@example.com");

// Implicit conversion to string
string emailStr = email;

// Explicit conversion from string
Email email2 = (Email)"another@example.com";
```

---

## Cpf (Brazilian)

Represents a Brazilian CPF (Cadastro de Pessoa Física) with validation.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create with validation (accepts formatted or unformatted)
var cpf = Cpf.Create("123.456.789-09");
var cpf2 = Cpf.Create("12345678909");

// Properties
Console.WriteLine(cpf.Value);       // 12345678909 (unformatted)
Console.WriteLine(cpf.Formatted);   // 123.456.789-09
Console.WriteLine(cpf.Unformatted); // 12345678909

// Safe parsing
if (Cpf.TryParse("123.456.789-09", out var result))
{
    Console.WriteLine($"Valid CPF: {result.Formatted}");
}

// Validation only
bool isValid = Cpf.IsValid("123.456.789-09");
```

**Note**: The validation includes check digit verification using the official CPF algorithm.

---

## Cnpj (Brazilian)

Represents a Brazilian CNPJ (Cadastro Nacional da Pessoa Jurídica) with validation.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create with validation
var cnpj = Cnpj.Create("12.345.678/0001-95");
var cnpj2 = Cnpj.Create("12345678000195");

// Properties
Console.WriteLine(cnpj.Value);       // 12345678000195
Console.WriteLine(cnpj.Formatted);   // 12.345.678/0001-95
Console.WriteLine(cnpj.Unformatted); // 12345678000195

// Safe parsing
if (Cnpj.TryParse("12.345.678/0001-95", out var result))
{
    Console.WriteLine($"Valid CNPJ: {result.Formatted}");
}

// Validation only
bool isValid = Cnpj.IsValid("12.345.678/0001-95");
```

---

## Money

Represents monetary values with currency support.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create with amount and currency
var price = Money.Create(99.99m, "USD");
var priceInReais = Money.Create(499.90m, "BRL");

// Properties
Console.WriteLine(price.Amount);   // 99.99
Console.WriteLine(price.Currency); // USD

// Arithmetic operations
var doubled = price * 2;           // $199.98
var discounted = price - 10m;      // $89.99
var total = price + price;         // $199.98

// Comparison
bool isMoreExpensive = price > Money.Create(50m, "USD"); // true

// Formatting
Console.WriteLine(price.ToString()); // USD 99.99

// Create zero value
var zero = Money.Zero("USD");
```

**Important**: Arithmetic operations require the same currency:

```csharp
var usd = Money.Create(100m, "USD");
var brl = Money.Create(100m, "BRL");

// This throws InvalidOperationException:
// var invalid = usd + brl;
```

---

## Address

Represents a physical address.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create a complete address
var address = Address.Create(
    street: "123 Main Street",
    number: "456",
    complement: "Apt 7B",
    neighborhood: "Downtown",
    city: "New York",
    state: "NY",
    zipCode: "10001",
    country: "USA"
);

// Properties
Console.WriteLine(address.Street);       // 123 Main Street
Console.WriteLine(address.Number);       // 456
Console.WriteLine(address.City);         // New York
Console.WriteLine(address.State);        // NY
Console.WriteLine(address.ZipCode);      // 10001
Console.WriteLine(address.Country);      // USA
Console.WriteLine(address.FullAddress);  // Complete formatted address
```

---

## DateRange

Represents a range between two dates.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create a date range
var range = DateRange.Create(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 12, 31)
);

// Properties
Console.WriteLine(range.Start);    // 2024-01-01
Console.WriteLine(range.End);      // 2024-12-31
Console.WriteLine(range.Duration); // TimeSpan

// Check if a date is within the range
var testDate = new DateTime(2024, 6, 15);
bool contains = range.Contains(testDate); // true

// Check if ranges overlap
var other = DateRange.Create(
    new DateTime(2024, 6, 1),
    new DateTime(2025, 1, 31)
);
bool overlaps = range.Overlaps(other); // true

// Create from duration
var week = DateRange.FromDuration(DateTime.Today, TimeSpan.FromDays(7));
```

---

## Percentage

Represents percentage values with conversion support.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create from percentage (0-100)
var discount = Percentage.FromPercent(25); // 25%

// Create from decimal (0-1)
var rate = Percentage.FromDecimal(0.25m); // 25%

// Properties
Console.WriteLine(discount.Value);     // 0.25 (decimal representation)
Console.WriteLine(discount.AsPercent); // 25 (percentage representation)

// Apply to a value
decimal price = 100m;
decimal discountAmount = discount.Apply(price); // 25

// Arithmetic
var total = discount + Percentage.FromPercent(10); // 35%
var half = discount / 2; // 12.5%

// Validation
bool valid = Percentage.IsValid(150); // false (> 100%)
```

---

## PhoneNumber

Represents international phone numbers.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Create with validation
var phone = PhoneNumber.Create("+1", "555", "1234567");

// Properties
Console.WriteLine(phone.CountryCode); // +1
Console.WriteLine(phone.AreaCode);    // 555
Console.WriteLine(phone.Number);      // 1234567
Console.WriteLine(phone.E164Format);  // +15551234567

// Brazilian format
var brazilian = PhoneNumber.Create("+55", "11", "999887766");
Console.WriteLine(brazilian.E164Format); // +5511999887766
```

---

## Creating Custom Value Objects

Extend `BaseVO` to create your own Value Objects:

```csharp
using Mvp24Hours.Core.ValueObjects;

public sealed class ProductCode : BaseVO, IEquatable<ProductCode>
{
    public string Category { get; }
    public int Sequence { get; }
    public string Value => $"{Category}-{Sequence:D4}";
    
    private ProductCode(string category, int sequence)
    {
        Category = category;
        Sequence = sequence;
    }
    
    public static ProductCode Create(string category, int sequence)
    {
        Guard.Against.NullOrEmpty(category, nameof(category));
        Guard.Against.NegativeOrZero(sequence, nameof(sequence));
        Guard.Against.LengthOutOfRange(category, 2, 2, nameof(category));
        
        return new ProductCode(category.ToUpper(), sequence);
    }
    
    public static bool TryParse(string value, out ProductCode result)
    {
        result = null!;
        if (string.IsNullOrEmpty(value)) return false;
        
        var parts = value.Split('-');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[1], out var sequence)) return false;
        
        try
        {
            result = Create(parts[0], sequence);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Category;
        yield return Sequence;
    }
    
    public bool Equals(ProductCode? other) => base.Equals(other);
    
    public override string ToString() => Value;
    
    public static implicit operator string(ProductCode code) => code.Value;
}
```

Usage:

```csharp
var code = ProductCode.Create("AB", 123);
Console.WriteLine(code.Value); // AB-0123

if (ProductCode.TryParse("XY-0456", out var parsed))
{
    Console.WriteLine(parsed.Category); // XY
}
```

---

## Best Practices

1. **Use Value Objects for domain concepts** - Email, Money, CPF are domain concepts, not just strings/decimals
2. **Validate on creation** - All validation happens in `Create` or constructor
3. **Make them immutable** - Value Objects should never change after creation
4. **Implement equality** - Two Value Objects with same values should be equal
5. **Provide `TryParse`** - Always offer safe parsing for user input

