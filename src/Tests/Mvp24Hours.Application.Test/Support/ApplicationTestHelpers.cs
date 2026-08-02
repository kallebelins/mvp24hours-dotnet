//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.Logic.Cache;
using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Application.Test.Support;

public class AppTestEntity : IEntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public object? EntityKey => Id;
}

public class AppTestEntityDto
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}

public class AppTestCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class AppTestUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ActiveAppTestEntitySpec : Specification<AppTestEntity>
{
    protected override Expression<Func<AppTestEntity, bool>> Criteria => e => e.Active;
}

public sealed class BulkTestDbContext(DbContextOptions options) : TestDbContext(options), IUnitOfWorkAsync
{
    private readonly Dictionary<Type, object> _repositories = [];
    private static readonly IOptions<EFCoreRepositoryOptions> RepositoryOptions =
        Microsoft.Extensions.Options.Options.Create(new EFCoreRepositoryOptions { MaxQtyByQueryPage = 100 });

    public IRepositoryAsync<T> GetRepository<T>()
        where T : class, IEntityBase
    {
        if (!_repositories.TryGetValue(typeof(T), out object? repository))
        {
            repository = new RepositoryAsync<T>(this, RepositoryOptions);
            _repositories[typeof(T)] = repository;
        }

        return (IRepositoryAsync<T>)repository;
    }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    public Task RollbackAsync()
    {
        ChangeTracker.Clear();
        return Task.CompletedTask;
    }

    public IDbConnection GetConnection()
    {
        return Database.GetDbConnection();
    }
}

public class AppTestEntityValidator : AbstractValidator<AppTestEntity>
{
    public AppTestEntityValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class TestAutoMapperProfile : Profile
{
    public TestAutoMapperProfile()
    {
        CreateMap<AppTestEntity, AppTestEntityDto>().ReverseMap();
    }
}

public class AppTestEntityDtoValidator : AbstractValidator<AppTestEntityDto>
{
    public AppTestEntityDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class AppTestCreateDtoValidator : AbstractValidator<AppTestCreateDto>
{
    public AppTestCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class AppTestUpdateDtoValidator : AbstractValidator<AppTestUpdateDto>
{
    public AppTestUpdateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class TestQueryService(IUnitOfWorkAsync unitOfWork) : QueryServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork)
{
}

public sealed class TestCommandService(IUnitOfWorkAsync unitOfWork, IValidator<AppTestEntity>? validator = null) : CommandServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, validator)
{
}

public sealed class TestApplicationServiceAsync(IUnitOfWorkAsync unitOfWork, IValidator<AppTestEntity>? validator = null) : ApplicationServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, validator)
{
}

public sealed class TestApplicationServiceWithDtoAsync(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    IValidator<AppTestEntity>? entityValidator = null,
    IValidator<AppTestEntityDto>? dtoValidator = null) : ApplicationServiceBaseWithDtoAsync<AppTestEntity, AppTestEntityDto, IUnitOfWorkAsync>(unitOfWork, mapper, entityValidator, dtoValidator)
{
}

public sealed class TestApplicationServiceWithSeparateDtosAsync(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    IValidator<AppTestEntity>? entityValidator = null,
    IValidator<AppTestCreateDto>? createValidator = null,
    IValidator<AppTestUpdateDto>? updateValidator = null)
        : ApplicationServiceBaseWithSeparateDtosAsync<AppTestEntity, AppTestEntityDto, AppTestCreateDto, AppTestUpdateDto, IUnitOfWorkAsync>(unitOfWork, mapper, entityValidator, createValidator, updateValidator)
{
}

public sealed class TestRepositoryService(IUnitOfWorkAsync unitOfWork, IValidator<AppTestEntity>? validator = null) : RepositoryServiceAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, validator)
{
}

public sealed class TestRepositoryPagingService(IUnitOfWorkAsync unitOfWork) : RepositoryPagingServiceAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork)
{
}

public sealed class TestApplicationService(IUnitOfWork unitOfWork, IValidator<AppTestEntity>? validator = null) : ApplicationServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork, validator)
{
}

public sealed class TestApplicationServiceWithDto(IUnitOfWork unitOfWork, IMapper mapper, IValidator<AppTestEntity>? entityValidator = null, IValidator<AppTestEntityDto>? dtoValidator = null) : ApplicationServiceBaseWithDto<AppTestEntity, AppTestEntityDto, IUnitOfWork>(unitOfWork, mapper, entityValidator, dtoValidator)
{
}

public sealed class TestApplicationServiceWithSeparateDtos(IUnitOfWork unitOfWork, IMapper mapper, IValidator<AppTestEntity>? entityValidator = null, IValidator<AppTestCreateDto>? createValidator = null, IValidator<AppTestUpdateDto>? updateValidator = null) : ApplicationServiceBaseWithSeparateDtos<AppTestEntity, AppTestEntityDto, AppTestCreateDto, AppTestUpdateDto, IUnitOfWork>(unitOfWork, mapper, entityValidator, createValidator, updateValidator)
{
}

public sealed class TestSyncQueryService(IUnitOfWork unitOfWork) : QueryServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork)
{
}

public sealed class TestSyncCommandService(IUnitOfWork unitOfWork, IValidator<AppTestEntity>? validator = null) : CommandServiceBase<AppTestEntity, IUnitOfWork>(unitOfWork, validator)
{
}

public sealed class TestSyncRepositoryService(IUnitOfWork unitOfWork, IValidator<AppTestEntity>? validator = null) : RepositoryService<AppTestEntity, IUnitOfWork>(unitOfWork, validator)
{
}

public sealed class TestSyncRepositoryPagingService(IUnitOfWork unitOfWork) : RepositoryPagingService<AppTestEntity, IUnitOfWork>(unitOfWork)
{
}

public sealed class TestCacheableQueryService(
    IUnitOfWorkAsync unitOfWork,
    IQueryCacheProvider cacheProvider,
    IQueryCacheKeyGenerator keyGenerator) : CacheableQueryServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, cacheProvider, keyGenerator, NullLogger<TestCacheableQueryService>.Instance)
{
    public void SetCacheEnabled(bool enabled)
    {
        CacheEnabled = enabled;
    }

    public bool IsCacheEnabled => CacheEnabled;

    public IDisposable DisableCacheForTest()
    {
        return DisableCache();
    }
}

public sealed class TestCacheableApplicationService(
    IUnitOfWorkAsync unitOfWork,
    IQueryCacheProvider cacheProvider,
    ICacheInvalidator cacheInvalidator,
    IQueryCacheKeyGenerator keyGenerator,
    IValidator<AppTestEntity>? validator = null) : CacheableApplicationServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, cacheProvider, cacheInvalidator, keyGenerator,
        NullLogger<TestCacheableApplicationService>.Instance, validator)
{
}

public sealed class TestEventAwareCommandService(
    IUnitOfWorkAsync unitOfWork,
    IApplicationEventDispatcher eventDispatcher,
    IValidator<AppTestEntity>? validator = null) : EventAwareCommandServiceBaseAsync<AppTestEntity, IUnitOfWorkAsync>(unitOfWork, eventDispatcher, validator)
{
    public void SetDispatchEvents(bool enabled)
    {
        DispatchEvents = enabled;
    }
}

public sealed class TestBulkDtoService(BulkTestDbContext dbContext, IMapper mapper, IValidator<AppTestEntityDto>? dtoValidator = null) : BulkCommandServiceWithDtoBaseAsync<TestEntity, AppTestEntityDto, BulkTestDbContext>(dbContext, mapper, dtoValidator)
{
}

public sealed class TestBulkSeparateDtosService(
    BulkTestDbContext dbContext,
    IMapper mapper,
    IValidator<AppTestCreateDto>? createValidator = null,
    IValidator<AppTestUpdateDto>? updateValidator = null)
        : BulkCommandServiceWithSeparateDtosBaseAsync<TestEntity, AppTestCreateDto, AppTestUpdateDto, BulkTestDbContext>(dbContext, mapper, createValidator, updateValidator)
{
}

public sealed class TestApplicationEvent : IApplicationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string Payload { get; init; } = string.Empty;
}

public sealed class CapturingEventHandler : IApplicationEventHandler<TestApplicationEvent>
{
    public List<TestApplicationEvent> Handled { get; } = [];

    public Task HandleAsync(TestApplicationEvent @event, CancellationToken cancellationToken = default)
    {
        Handled.Add(@event);
        return Task.CompletedTask;
    }
}

public sealed class FailingEventHandler : IApplicationEventHandler<TestApplicationEvent>
{
    public Task HandleAsync(TestApplicationEvent @event, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Handler failed");
    }
}

public sealed class TestCacheableQuery : ICacheableQuery
{
    public int CategoryId { get; init; }
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
    public bool UseSlidingExpiration => true;
    public string GetCacheKey()
    {
        return $"category_{CategoryId}";
    }

    public string CacheRegion => "Products";
}

public static class ApplicationTestHelpers
{
    public static (Mock<IUnitOfWorkAsync> UnitOfWork, Mock<IRepositoryAsync<TEntity>> Repository) CreateRepositoryMocks<TEntity>()
        where TEntity : class, IEntityBase
    {
        var repository = new Mock<IRepositoryAsync<TEntity>>();
        var unitOfWork = new Mock<IUnitOfWorkAsync>();
        unitOfWork.Setup(u => u.GetRepository<TEntity>()).Returns(() => repository.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return (unitOfWork, repository);
    }

    public static (Mock<IUnitOfWork> UnitOfWork, Mock<IRepository<TEntity>> Repository) CreateSyncRepositoryMocks<TEntity>()
        where TEntity : class, IEntityBase
    {
        var repository = new Mock<IRepository<TEntity>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.GetRepository<TEntity>()).Returns(() => repository.Object);
        unitOfWork.Setup(u => u.SaveChanges(It.IsAny<CancellationToken>())).Returns(1);
        return (unitOfWork, repository);
    }

    public static void SetupListAny<TEntity>(Mock<IRepositoryAsync<TEntity>> repository, bool value)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.ListAnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(value);
    }

    public static void SetupListAny<TEntity>(Mock<IRepository<TEntity>> repository, bool value)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.ListAny()).Returns(value);
    }

    public static void SetupListCount<TEntity>(Mock<IRepositoryAsync<TEntity>> repository, int count)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.ListCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(count);
    }

    public static void SetupListCount<TEntity>(Mock<IRepository<TEntity>> repository, int count)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.ListCount()).Returns(count);
    }

    public static void SetupList<TEntity>(Mock<IRepositoryAsync<TEntity>> repository, IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        repository.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
    }

    public static void SetupList<TEntity>(Mock<IRepository<TEntity>> repository, IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.List(It.IsAny<IPagingCriteria?>())).Returns(items);
        repository.Setup(r => r.List()).Returns(items);
    }

    public static void SetupGetById<TEntity>(Mock<IRepositoryAsync<TEntity>> repository, object id, TEntity? entity)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
    }

    public static void SetupGetById<TEntity>(Mock<IRepository<TEntity>> repository, object id, TEntity? entity)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetById(id, It.IsAny<IPagingCriteria?>())).Returns(entity);
        repository.Setup(r => r.GetById(id)).Returns(entity);
    }

    public static void SetupGetBy<TEntity>(
        Mock<IRepositoryAsync<TEntity>> repository,
        Expression<Func<TEntity, bool>> clause,
        IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetByAsync(clause, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        repository.Setup(r => r.GetByAsync(clause, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        repository.Setup(r => r.GetByAnyAsync(clause, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Any());
        repository.Setup(r => r.GetByCountAsync(clause, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count);
    }

    public static void SetupGetBy<TEntity>(
        Mock<IRepository<TEntity>> repository,
        Expression<Func<TEntity, bool>> clause,
        IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetBy(clause, It.IsAny<IPagingCriteria?>())).Returns(items);
        repository.Setup(r => r.GetBy(clause)).Returns(items);
        repository.Setup(r => r.GetByAny(clause)).Returns(items.Any());
        repository.Setup(r => r.GetByCount(clause)).Returns(items.Count);
    }

    public static IMapper CreateTestMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AppTestEntityDto, TestEntity>()
                .ForMember(d => d.Id, o => o.Ignore());
            cfg.CreateMap<AppTestCreateDto, TestEntity>()
                .ForMember(d => d.Id, o => o.Ignore());
            cfg.CreateMap<AppTestUpdateDto, TestEntity>();
        }, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    public static IMapper CreateAppEntityMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AppTestEntity, AppTestEntityDto>().ReverseMap();
            cfg.CreateMap<AppTestCreateDto, AppTestEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Active, o => o.MapFrom(_ => true));
            cfg.CreateMap<AppTestUpdateDto, AppTestEntity>();
            cfg.CreateMap<AppTestEntity, AppTestUpdateDto>();
        }, NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    public static void SetupGetByAnyExpression<TEntity>(
        Mock<IRepositoryAsync<TEntity>> repository,
        IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        repository.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        repository.Setup(r => r.GetByAnyAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Any());
        repository.Setup(r => r.GetByCountAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count);
    }

    public static void SetupGetByAnyExpression<TEntity>(
        Mock<IRepository<TEntity>> repository,
        IList<TEntity> items)
        where TEntity : class, IEntityBase
    {
        repository.Setup(r => r.GetBy(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<IPagingCriteria?>())).Returns(items);
        repository.Setup(r => r.GetBy(It.IsAny<Expression<Func<TEntity, bool>>>())).Returns(items);
        repository.Setup(r => r.GetByAny(It.IsAny<Expression<Func<TEntity, bool>>>())).Returns(items.Any());
        repository.Setup(r => r.GetByCount(It.IsAny<Expression<Func<TEntity, bool>>>())).Returns(items.Count);
    }

    public static void SetupReadOnlySpecification<TEntity, TSpec>(
        Mock<IRepositoryAsync<TEntity>> repository,
        bool anyResult = true,
        TEntity? firstResult = null)
        where TEntity : class, IEntityBase
        where TSpec : class, ISpecificationQuery<TEntity>
    {
        Mock<IReadOnlyRepositoryAsync<TEntity>> readOnly = repository.As<IReadOnlyRepositoryAsync<TEntity>>();
        readOnly.Setup(r => r.AnyBySpecificationAsync(It.IsAny<TSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(anyResult);
        if (firstResult != null)
        {
            readOnly.Setup(r => r.GetFirstBySpecificationAsync(It.IsAny<TSpec>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstResult);
        }
    }

    public static void SetupReadOnlySpecification<TEntity, TSpec>(
        Mock<IRepository<TEntity>> repository,
        bool anyResult = true,
        TEntity? firstResult = null)
        where TEntity : class, IEntityBase
        where TSpec : class, ISpecificationQuery<TEntity>
    {
        Mock<IReadOnlyRepository<TEntity>> readOnly = repository.As<IReadOnlyRepository<TEntity>>();
        readOnly.Setup(r => r.AnyBySpecification(It.IsAny<TSpec>())).Returns(anyResult);
        if (firstResult != null)
        {
            readOnly.Setup(r => r.GetFirstBySpecification(It.IsAny<TSpec>())).Returns(firstResult);
        }
    }

    public static InMemoryQueryCacheProvider CreateInMemoryQueryCacheProvider()
    {
        return new();
    }

    public static QueryCacheProvider CreateQueryCacheProvider(
        out MemoryDistributedCache distributedCache,
        IMemoryCache? memoryCache = null)
    {
        distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new QueryCacheProvider(
            distributedCache,
            NullLogger<QueryCacheProvider>.Instance,
            Options.Create(new QueryCacheOptions { EnableL1Cache = memoryCache != null }),
            memoryCache);
    }

    public static CacheInvalidator CreateCacheInvalidator(IQueryCacheProvider cacheProvider)
    {
        return new(cacheProvider, new QueryCacheKeyGenerator(), NullLogger<CacheInvalidator>.Instance);
    }

    public static ServiceProvider CreateEventDispatcherServices(
        Action<ApplicationEventDispatcherOptions>? configure = null,
        params object[] handlers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ApplicationEventDispatcherOptions>().Configure(o =>
        {
            o.UseOutbox = false;
            o.ContinueOnError = true;
            configure?.Invoke(o);
        });
        foreach (object handler in handlers)
        {
            services.AddSingleton(typeof(IApplicationEventHandler<TestApplicationEvent>), handler);
        }
        services.AddSingleton<IApplicationEventDispatcher, ApplicationEventDispatcher>();
        return services.BuildServiceProvider();
    }
}

public sealed class InMemoryQueryCacheProvider : IQueryCacheProvider
{
    private readonly ConcurrentDictionary<string, object?> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out object? value);
        return Task.FromResult(value is T typed ? typed : default);
    }

    public Task SetAsync<T>(string key, T value, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out object? existing) && existing is T typed)
        {
            return typed;
        }

        T value = await factory();
        _store[key] = value;
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        foreach (string key in _store.Keys.Where(k => k.Contains(region, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        string prefix = pattern.TrimEnd('*');
        foreach (string key in _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(key));
    }
}
