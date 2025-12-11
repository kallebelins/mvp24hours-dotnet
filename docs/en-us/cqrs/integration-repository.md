# Integration with Repository Pattern

## Overview

The Mediator integrates naturally with the existing Repository Pattern in Mvp24Hours, allowing clear separation between application logic and data access.

## Repository vs Mediator

| Aspect | Repository | Mediator |
|--------|-----------|----------|
| **Responsibility** | Data access | Use case orchestration |
| **Operations** | Generic CRUD | Specific Commands and Queries |
| **Abstraction** | Persistence | Application flow |
| **Composition** | Simple operations | Pipeline with behaviors |

## Repository Interfaces

### IRepository

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
```

### IRepositoryAsync

```csharp
public interface IRepositoryAsync<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
```

## Using Repository in Handlers

### Command Handler with Repository

```csharp
public class CreateProductCommandHandler 
    : IMediatorCommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CreateProductCommandHandler(
        IRepository<Product> productRepository,
        IUnitOfWorkAsync unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand request, 
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Category = request.Category
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
```

### Query Handler with Repository

```csharp
public class GetProductByIdQueryHandler 
    : IMediatorQueryHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IRepositoryAsync<Product> _productRepository;

    public GetProductByIdQueryHandler(IRepositoryAsync<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(
        GetProductByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(
            request.ProductId, 
            cancellationToken);

        return product is null ? null : ProductDto.FromEntity(product);
    }
}
```

### Query Handler with Filters

```csharp
public class GetProductsQueryHandler 
    : IMediatorQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IRepositoryAsync<Product> _productRepository;

    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request, 
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.ListAsync(
            p => (request.Category == null || p.Category == request.Category)
                 && (request.MinPrice == null || p.Price >= request.MinPrice)
                 && (request.MaxPrice == null || p.Price <= request.MaxPrice),
            cancellationToken);

        return products.Select(ProductDto.FromEntity).ToList();
    }
}
```

## Recommended Pattern

### Data Flow

```
Controller → Mediator → Handler → Repository → Database
                ↓
          [Behaviors]
          - Validation
          - Transaction
          - Caching
          - Logging
```

### Complete Example

```csharp
// Command
public record UpdateProductCommand : IMediatorCommand<ProductDto>, ITransactional
{
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
}

// Validator
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Handler
public class UpdateProductCommandHandler 
    : IMediatorCommandHandler<UpdateProductCommand, ProductDto>
{
    private readonly IRepositoryAsync<Product> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public async Task<ProductDto> Handle(
        UpdateProductCommand request, 
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(
            request.ProductId, 
            cancellationToken);

        if (product is null)
            throw new NotFoundException("Product", request.ProductId);

        product.Name = request.Name;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        _repository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product);
    }
}

// Controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id, 
        UpdateProductRequest request)
    {
        var command = new UpdateProductCommand
        {
            ProductId = id,
            Name = request.Name,
            Price = request.Price
        };

        var result = await _mediator.SendAsync(command);
        return Ok(result);
    }
}
```

## Best Practices

1. **Repository for Data**: Use repository only for data operations
2. **Handler for Logic**: Keep business logic in the handler
3. **UnitOfWork for Transactions**: Use with TransactionBehavior
4. **Specific Interfaces**: Prefer IRepositoryAsync for async operations
5. **DTOs in Return**: Return DTOs, not entities

