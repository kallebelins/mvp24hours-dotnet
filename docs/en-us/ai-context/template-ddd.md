# Domain-Driven Design (DDD) Template

> **AI Agent Instruction**: Use this template for complex business domains where the domain model is central to the application. DDD focuses on the core domain and domain logic, using ubiquitous language shared by developers and domain experts.

---

## When to Use DDD

### Recommended Scenarios
- Complex business domains with intricate rules
- Projects with active domain expert involvement
- Long-term enterprise applications
- Systems requiring deep business logic modeling
- Collaborative teams valuing ubiquitous language

### Not Recommended
- Simple CRUD applications
- Technical-focused projects (no complex domain)
- Short-term projects with limited scope
- Teams without domain expert access

---

## Core Concepts

### Strategic Design
- **Bounded Context**: Explicit boundary within which a domain model exists
- **Ubiquitous Language**: Common language shared by team and domain experts
- **Context Map**: Visual representation of bounded contexts relationships

### Tactical Design
- **Entities**: Objects with identity that persists over time
- **Value Objects**: Immutable objects without identity
- **Aggregates**: Cluster of entities and value objects with consistency boundary
- **Aggregate Root**: Entry point to an aggregate
- **Domain Services**: Operations that don't belong to entities
- **Domain Events**: Record of something significant that happened
- **Repositories**: Abstraction for aggregate persistence
- **Factories**: Create complex aggregates

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Domain/
    │   ├── ProjectName.Domain.csproj
    │   ├── Aggregates/
    │   │   ├── CustomerAggregate/
    │   │   │   ├── Customer.cs (Aggregate Root)
    │   │   │   ├── Contact.cs (Entity)
    │   │   │   ├── Address.cs (Value Object)
    │   │   │   └── CustomerType.cs (Enum)
    │   │   └── OrderAggregate/
    │   │       ├── Order.cs (Aggregate Root)
    │   │       ├── OrderItem.cs (Entity)
    │   │       ├── OrderStatus.cs (Enum)
    │   │       └── Money.cs (Value Object)
    │   ├── Common/
    │   │   ├── Entity.cs
    │   │   ├── AggregateRoot.cs
    │   │   ├── ValueObject.cs
    │   │   └── DomainEvent.cs
    │   ├── DomainServices/
    │   │   ├── ICustomerDomainService.cs
    │   │   ├── CustomerDomainService.cs
    │   │   └── IPricingService.cs
    │   ├── Events/
    │   │   ├── CustomerCreatedEvent.cs
    │   │   ├── CustomerDeactivatedEvent.cs
    │   │   ├── OrderPlacedEvent.cs
    │   │   └── OrderCancelledEvent.cs
    │   ├── Exceptions/
    │   │   ├── DomainException.cs
    │   │   ├── InvalidCustomerStateException.cs
    │   │   └── OrderCannotBeCancelledException.cs
    │   ├── Repositories/
    │   │   ├── ICustomerRepository.cs
    │   │   └── IOrderRepository.cs
    │   ├── Factories/
    │   │   ├── ICustomerFactory.cs
    │   │   └── IOrderFactory.cs
    │   └── Specifications/
    │       ├── ISpecification.cs
    │       ├── ActiveCustomerSpec.cs
    │       └── OrderByDateRangeSpec.cs
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Commands/
    │   │   ├── CreateCustomerCommand.cs
    │   │   ├── PlaceOrderCommand.cs
    │   │   └── CancelOrderCommand.cs
    │   ├── Queries/
    │   │   ├── GetCustomerQuery.cs
    │   │   └── GetOrdersByCustomerQuery.cs
    │   ├── Handlers/
    │   │   ├── CreateCustomerHandler.cs
    │   │   ├── PlaceOrderHandler.cs
    │   │   └── GetCustomerHandler.cs
    │   ├── DTOs/
    │   │   ├── CustomerDto.cs
    │   │   └── OrderDto.cs
    │   └── EventHandlers/
    │       ├── CustomerCreatedEventHandler.cs
    │       └── OrderPlacedEventHandler.cs
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Persistence/
    │   │   ├── DataContext.cs
    │   │   ├── Repositories/
    │   │   │   ├── CustomerRepository.cs
    │   │   │   └── OrderRepository.cs
    │   │   └── Configurations/
    │   │       ├── CustomerConfiguration.cs
    │   │       └── OrderConfiguration.cs
    │   ├── Factories/
    │   │   ├── CustomerFactory.cs
    │   │   └── OrderFactory.cs
    │   └── DomainServices/
    │       └── PricingService.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Controllers/
        │   ├── CustomersController.cs
        │   └── OrdersController.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Namespaces

```csharp
// Domain (Rich Domain Model)
ProjectName.Domain.Aggregates.CustomerAggregate
ProjectName.Domain.Aggregates.OrderAggregate
ProjectName.Domain.Common
ProjectName.Domain.DomainServices
ProjectName.Domain.Events
ProjectName.Domain.Exceptions
ProjectName.Domain.Repositories
ProjectName.Domain.Factories
ProjectName.Domain.Specifications

// Application (Orchestration)
ProjectName.Application.Commands
ProjectName.Application.Queries
ProjectName.Application.Handlers
ProjectName.Application.DTOs
ProjectName.Application.EventHandlers

// Infrastructure (Implementation)
ProjectName.Infrastructure.Persistence
ProjectName.Infrastructure.Persistence.Repositories
ProjectName.Infrastructure.Persistence.Configurations
ProjectName.Infrastructure.Factories
ProjectName.Infrastructure.DomainServices

// WebAPI
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.Extensions
```

---

## Domain Layer - Common Building Blocks

### Entity Base

```csharp
// Domain/Common/Entity.cs
namespace ProjectName.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### Aggregate Root

```csharp
// Domain/Common/AggregateRoot.cs
namespace ProjectName.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    public int Version { get; protected set; }

    protected void IncrementVersion() => Version++;
}
```

### Value Object Base

```csharp
// Domain/Common/ValueObject.cs
namespace ProjectName.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
```

### Domain Event Base

```csharp
// Domain/Common/DomainEvent.cs
namespace ProjectName.Domain.Common;

public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

---

## Customer Aggregate

### Aggregate Root

```csharp
// Domain/Aggregates/CustomerAggregate/Customer.cs
using ProjectName.Domain.Common;
using ProjectName.Domain.Events;
using ProjectName.Domain.Exceptions;

namespace ProjectName.Domain.Aggregates.CustomerAggregate;

public class Customer : AggregateRoot<int>
{
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public CustomerType Type { get; private set; }
    public bool Active { get; private set; }
    public DateTime Created { get; private set; }
    public Address? BillingAddress { get; private set; }

    private readonly List<Contact> _contacts = new();
    public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();

    private Customer() { } // EF Core

    // Factory method - encapsulates creation logic
    public static Customer Create(string name, string email, CustomerType type = CustomerType.Regular)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required");

        var customer = new Customer
        {
            Name = name,
            Email = new Email(email),
            Type = type,
            Active = true,
            Created = DateTime.UtcNow
        };

        customer.AddDomainEvent(new CustomerCreatedEvent(customer.Id, customer.Name, customer.Email.Value));
        return customer;
    }

    // Behavior methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required");
        
        Name = name;
        IncrementVersion();
    }

    public void UpdateEmail(string email)
    {
        Email = new Email(email);
        IncrementVersion();
    }

    public void SetBillingAddress(Address address)
    {
        BillingAddress = address ?? throw new DomainException("Address cannot be null");
        IncrementVersion();
    }

    public void UpgradeToPreferred()
    {
        if (Type == CustomerType.Preferred)
            throw new InvalidCustomerStateException("Customer is already preferred");
        
        Type = CustomerType.Preferred;
        IncrementVersion();
    }

    public void Deactivate()
    {
        if (!Active)
            throw new InvalidCustomerStateException("Customer is already inactive");

        Active = false;
        AddDomainEvent(new CustomerDeactivatedEvent(Id, Name));
        IncrementVersion();
    }

    public void Activate()
    {
        if (Active)
            throw new InvalidCustomerStateException("Customer is already active");

        Active = true;
        IncrementVersion();
    }

    // Contact management within aggregate boundary
    public Contact AddContact(string name, string phone, string email, ContactType type)
    {
        if (_contacts.Count >= 10)
            throw new DomainException("Maximum contacts limit reached");

        var contact = new Contact(name, phone, email, type);
        _contacts.Add(contact);
        IncrementVersion();
        return contact;
    }

    public void RemoveContact(int contactId)
    {
        var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact == null)
            throw new DomainException($"Contact {contactId} not found");

        _contacts.Remove(contact);
        IncrementVersion();
    }
}
```

### Entity within Aggregate

```csharp
// Domain/Aggregates/CustomerAggregate/Contact.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Aggregates.CustomerAggregate;

public class Contact : Entity<int>
{
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public ContactType Type { get; private set; }

    private Contact() { } // EF Core

    internal Contact(string name, string phone, string email, ContactType type)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Type = type;
    }

    public void Update(string name, string phone, string email, ContactType type)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Type = type;
    }
}

public enum ContactType
{
    Primary,
    Billing,
    Shipping,
    Technical
}
```

### Value Objects

```csharp
// Domain/Aggregates/CustomerAggregate/Email.cs
using ProjectName.Domain.Common;
using ProjectName.Domain.Exceptions;

namespace ProjectName.Domain.Aggregates.CustomerAggregate;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required");
        
        if (!value.Contains('@'))
            throw new DomainException("Invalid email format");

        Value = value.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
}

// Domain/Aggregates/CustomerAggregate/Address.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Aggregates.CustomerAggregate;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}

// Domain/Aggregates/CustomerAggregate/CustomerType.cs
namespace ProjectName.Domain.Aggregates.CustomerAggregate;

public enum CustomerType
{
    Regular,
    Preferred,
    VIP
}
```

---

## Order Aggregate

```csharp
// Domain/Aggregates/OrderAggregate/Order.cs
using ProjectName.Domain.Common;
using ProjectName.Domain.Events;
using ProjectName.Domain.Exceptions;

namespace ProjectName.Domain.Aggregates.OrderAggregate;

public class Order : AggregateRoot<int>
{
    public int CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime OrderDate { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public Address ShippingAddress { get; private set; } = null!;

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(int customerId, Address shippingAddress)
    {
        var order = new Order
        {
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            TotalAmount = new Money(0)
        };
        return order;
    }

    public void AddItem(int productId, string productName, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify a non-draft order");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var item = new OrderItem(productId, productName, quantity, unitPrice);
            _items.Add(item);
        }

        RecalculateTotal();
        IncrementVersion();
    }

    public void RemoveItem(int productId)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify a non-draft order");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new DomainException($"Product {productId} not found in order");

        _items.Remove(item);
        RecalculateTotal();
        IncrementVersion();
    }

    public void Place()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only draft orders can be placed");

        if (!_items.Any())
            throw new DomainException("Cannot place an empty order");

        Status = OrderStatus.Placed;
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount.Amount));
        IncrementVersion();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Placed)
            throw new DomainException("Only placed orders can be confirmed");

        Status = OrderStatus.Confirmed;
        IncrementVersion();
    }

    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
        IncrementVersion();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new OrderCannotBeCancelledException(Id, Status);

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id, CustomerId));
        IncrementVersion();
    }

    private void RecalculateTotal()
    {
        var total = _items.Sum(i => i.TotalPrice.Amount);
        TotalAmount = new Money(total);
    }
}

// Domain/Aggregates/OrderAggregate/OrderItem.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Aggregates.OrderAggregate;

public class OrderItem : Entity<int>
{
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money TotalPrice => new(UnitPrice.Amount * Quantity);

    private OrderItem() { } // EF Core

    internal OrderItem(int productId, string productName, int quantity, Money unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void IncreaseQuantity(int quantity)
    {
        Quantity += quantity;
    }
}

// Domain/Aggregates/OrderAggregate/OrderStatus.cs
namespace ProjectName.Domain.Aggregates.OrderAggregate;

public enum OrderStatus
{
    Draft,
    Placed,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

// Domain/Aggregates/OrderAggregate/Money.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Aggregates.OrderAggregate;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot operate on different currencies");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## Domain Events

```csharp
// Domain/Events/CustomerCreatedEvent.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Events;

public record CustomerCreatedEvent(int CustomerId, string Name, string Email) : DomainEvent;

// Domain/Events/CustomerDeactivatedEvent.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Events;

public record CustomerDeactivatedEvent(int CustomerId, string Name) : DomainEvent;

// Domain/Events/OrderPlacedEvent.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Events;

public record OrderPlacedEvent(int OrderId, int CustomerId, decimal TotalAmount) : DomainEvent;

// Domain/Events/OrderCancelledEvent.cs
using ProjectName.Domain.Common;

namespace ProjectName.Domain.Events;

public record OrderCancelledEvent(int OrderId, int CustomerId) : DomainEvent;
```

---

## Domain Services

```csharp
// Domain/DomainServices/ICustomerDomainService.cs
using ProjectName.Domain.Aggregates.CustomerAggregate;

namespace ProjectName.Domain.DomainServices;

public interface ICustomerDomainService
{
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
    Task<Customer?> GetByEmailAsync(string email);
}

// Domain/DomainServices/IPricingService.cs
using ProjectName.Domain.Aggregates.CustomerAggregate;
using ProjectName.Domain.Aggregates.OrderAggregate;

namespace ProjectName.Domain.DomainServices;

public interface IPricingService
{
    Money CalculateDiscount(Order order, CustomerType customerType);
    Money ApplyTax(Money amount, string country);
}
```

---

## Repository Interfaces

```csharp
// Domain/Repositories/ICustomerRepository.cs
using ProjectName.Domain.Aggregates.CustomerAggregate;

namespace ProjectName.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdWithContactsAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    void Remove(Customer customer);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Domain/Repositories/IOrderRepository.cs
using ProjectName.Domain.Aggregates.OrderAggregate;

namespace ProjectName.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Specification Pattern

```csharp
// Domain/Specifications/ISpecification.cs
using System.Linq.Expressions;

namespace ProjectName.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}

// Domain/Specifications/ActiveCustomerSpec.cs
using System.Linq.Expressions;
using ProjectName.Domain.Aggregates.CustomerAggregate;

namespace ProjectName.Domain.Specifications;

public class ActiveCustomerSpec : ISpecification<Customer>
{
    public Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Active;
    }

    public bool IsSatisfiedBy(Customer entity)
    {
        return entity.Active;
    }
}

// Domain/Specifications/OrderByDateRangeSpec.cs
using System.Linq.Expressions;
using ProjectName.Domain.Aggregates.OrderAggregate;

namespace ProjectName.Domain.Specifications;

public class OrderByDateRangeSpec : ISpecification<Order>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public OrderByDateRangeSpec(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.OrderDate >= _startDate && order.OrderDate <= _endDate;
    }

    public bool IsSatisfiedBy(Order entity)
    {
        return entity.OrderDate >= _startDate && entity.OrderDate <= _endDate;
    }
}
```

---

## Application Layer - Command Handler

```csharp
// Application/Handlers/CreateCustomerHandler.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Application.Commands;
using ProjectName.Application.DTOs;
using ProjectName.Domain.Aggregates.CustomerAggregate;
using ProjectName.Domain.DomainServices;
using ProjectName.Domain.Repositories;

namespace ProjectName.Application.Handlers;

public class CreateCustomerHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerDomainService _customerDomainService;

    public CreateCustomerHandler(
        ICustomerRepository customerRepository,
        ICustomerDomainService customerDomainService)
    {
        _customerRepository = customerRepository;
        _customerDomainService = customerDomainService;
    }

    public async Task<IBusinessResult<CustomerDto>> HandleAsync(
        CreateCustomerCommand command, 
        CancellationToken cancellationToken)
    {
        // Business rule: email must be unique
        var isEmailUnique = await _customerDomainService.IsEmailUniqueAsync(command.Email);
        if (!isEmailUnique)
            return default(CustomerDto).ToBusiness("Email already exists");

        // Create aggregate through factory method
        var customer = Customer.Create(command.Name, command.Email, command.Type);

        // Persist
        await _customerRepository.AddAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        var dto = new CustomerDto(customer.Id, customer.Name, customer.Email.Value, customer.Type.ToString(), customer.Active);
        return dto.ToBusiness();
    }
}
```

---

## Infrastructure - Repository Implementation

```csharp
// Infrastructure/Persistence/Repositories/CustomerRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.Aggregates.CustomerAggregate;
using ProjectName.Domain.Repositories;

namespace ProjectName.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DataContext _context;

    public CustomerRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Customer?> GetByIdWithContactsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public void Remove(Customer customer)
    {
        _context.Customers.Remove(customer);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Entity Configuration (EF Core)

```csharp
// Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Domain.Aggregates.CustomerAggregate;

namespace ProjectName.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Value Object as Owned Type
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(255);
        });

        builder.OwnsOne(c => c.BillingAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("BillingStreet").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("BillingCity").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("BillingState").HasMaxLength(50);
            address.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(50);
            address.Property(a => a.ZipCode).HasColumnName("BillingZipCode").HasMaxLength(20);
        });

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Child entities within aggregate
        builder.HasMany(c => c.Contacts)
            .WithOne()
            .HasForeignKey("CustomerId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Clean Architecture Template](template-clean-architecture.md)
- [Hexagonal Architecture Template](template-hexagonal.md)
- [CQRS Template](template-cqrs.md)

