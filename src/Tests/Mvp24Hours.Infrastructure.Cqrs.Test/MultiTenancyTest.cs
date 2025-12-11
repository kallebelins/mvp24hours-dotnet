//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Behaviors;
using Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for multi-tenancy features.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class MultiTenancyTest
{
    #region [ TenantContext Tests ]

    [Fact, Priority(1)]
    public void TenantContext_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var properties = new Dictionary<string, object?> { { "Feature1", true }, { "MaxUsers", 100 } };
        var tenant = new TenantContext(
            tenantId: "tenant-1",
            tenantName: "Tenant One",
            connectionString: "Server=localhost;Database=Tenant1;",
            schema: "tenant1",
            properties: properties);

        // Assert
        Assert.Equal("tenant-1", tenant.TenantId);
        Assert.Equal("Tenant One", tenant.TenantName);
        Assert.Equal("Server=localhost;Database=Tenant1;", tenant.ConnectionString);
        Assert.Equal("tenant1", tenant.Schema);
        Assert.True(tenant.HasTenant);
        Assert.True(tenant.GetProperty<bool>("Feature1"));
        Assert.Equal(100, tenant.GetProperty<int>("MaxUsers"));
    }

    [Fact, Priority(2)]
    public void TenantContext_Empty_ShouldHaveNoTenant()
    {
        // Arrange
        var tenant = TenantContext.Empty;

        // Assert
        Assert.Null(tenant.TenantId);
        Assert.Null(tenant.TenantName);
        Assert.False(tenant.HasTenant);
    }

    [Fact, Priority(3)]
    public void TenantContext_FromId_ShouldCreateWithIdOnly()
    {
        // Arrange
        var tenant = TenantContext.FromId("tenant-123");

        // Assert
        Assert.Equal("tenant-123", tenant.TenantId);
        Assert.Null(tenant.TenantName);
        Assert.True(tenant.HasTenant);
    }

    [Fact, Priority(4)]
    public void TenantContext_GetProperty_ShouldReturnDefaultForMissing()
    {
        // Arrange
        var tenant = TenantContext.FromId("tenant-1");

        // Assert
        Assert.Null(tenant.GetProperty<string>("NonExistent"));
        Assert.Equal(0, tenant.GetProperty<int>("NonExistent"));
        Assert.Equal(42, tenant.GetProperty("NonExistent", 42));
    }

    #endregion

    #region [ TenantContextAccessor Tests ]

    [Fact, Priority(10)]
    public void TenantContextAccessor_ShouldStoreAndRetrieveContext()
    {
        // Arrange
        var accessor = new TenantContextAccessor();
        var tenant = TenantContext.FromIdAndName("tenant-1", "Tenant One");

        // Act
        accessor.Context = tenant;

        // Assert
        Assert.NotNull(accessor.Context);
        Assert.Equal("tenant-1", accessor.Context.TenantId);
        Assert.Equal("Tenant One", accessor.Context.TenantName);
    }

    [Fact, Priority(11)]
    public void TenantContextAccessor_ShouldClearContext()
    {
        // Arrange
        var accessor = new TenantContextAccessor();
        accessor.Context = TenantContext.FromId("tenant-1");

        // Act
        accessor.Context = null;

        // Assert
        Assert.Null(accessor.Context);
    }

    #endregion

    #region [ InMemoryTenantStore Tests ]

    [Fact, Priority(20)]
    public async Task InMemoryTenantStore_ShouldAddAndRetrieveTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new TenantContext("tenant-1", "Tenant One");
        store.AddOrUpdate(tenant);

        // Act
        var retrieved = await store.GetByIdAsync("tenant-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("tenant-1", retrieved.TenantId);
        Assert.Equal("Tenant One", retrieved.TenantName);
    }

    [Fact, Priority(21)]
    public async Task InMemoryTenantStore_ShouldRetrieveByName()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new TenantContext("tenant-1", "Tenant One");
        store.AddOrUpdate(tenant);

        // Act
        var retrieved = await store.GetByNameAsync("Tenant One");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("tenant-1", retrieved.TenantId);
    }

    [Fact, Priority(22)]
    public async Task InMemoryTenantStore_ShouldReturnNullForNonExistent()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var retrieved = await store.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact, Priority(23)]
    public async Task InMemoryTenantStore_ShouldGetAll()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddOrUpdate(new TenantContext("tenant-1", "Tenant One"));
        store.AddOrUpdate(new TenantContext("tenant-2", "Tenant Two"));

        // Act
        var all = (await store.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, all.Count);
    }

    [Fact, Priority(24)]
    public void InMemoryTenantStore_ShouldRemoveTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddOrUpdate(new TenantContext("tenant-1", "Tenant One"));

        // Act
        var removed = store.Remove("tenant-1");

        // Assert
        Assert.True(removed);
    }

    #endregion

    #region [ TenantFilter Tests ]

    [Fact, Priority(30)]
    public void TenantFilter_ShouldReturnCurrentTenantId()
    {
        // Arrange
        var accessor = new TenantContextAccessor { Context = TenantContext.FromId("tenant-1") };
        var filter = new TenantFilter(accessor);

        // Assert
        Assert.Equal("tenant-1", filter.CurrentTenantId);
        Assert.True(filter.ShouldFilter);
    }

    [Fact, Priority(31)]
    public void TenantFilter_ShouldNotFilterWhenNoTenant()
    {
        // Arrange
        var accessor = new TenantContextAccessor { Context = null };
        var filter = new TenantFilter(accessor);

        // Assert
        Assert.Null(filter.CurrentTenantId);
        Assert.False(filter.ShouldFilter);
    }

    [Fact, Priority(32)]
    public void TenantFilter_DisableFilter_ShouldTemporarilyDisable()
    {
        // Arrange
        var accessor = new TenantContextAccessor { Context = TenantContext.FromId("tenant-1") };
        var filter = new TenantFilter(accessor);

        // Act
        Assert.True(filter.ShouldFilter);
        using (filter.DisableFilter())
        {
            Assert.False(filter.ShouldFilter);
        }
        Assert.True(filter.ShouldFilter);
    }

    #endregion

    #region [ TenantQueryExtensions Tests ]

    [Fact, Priority(40)]
    public void FilterByTenant_ShouldFilterQueryable()
    {
        // Arrange
        var entities = new List<TenantEntity>
        {
            new() { Id = 1, Name = "Entity 1", TenantId = "tenant-1" },
            new() { Id = 2, Name = "Entity 2", TenantId = "tenant-2" },
            new() { Id = 3, Name = "Entity 3", TenantId = "tenant-1" }
        }.AsQueryable();

        // Act
        var filtered = entities.FilterByTenant("tenant-1").ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, e => Assert.Equal("tenant-1", e.TenantId));
    }

    [Fact, Priority(41)]
    public void FilterByTenant_ShouldReturnEmptyWhenNoTenant()
    {
        // Arrange
        var entities = new List<TenantEntity>
        {
            new() { Id = 1, Name = "Entity 1", TenantId = "tenant-1" }
        }.AsQueryable();

        // Act
        var filtered = entities.FilterByTenant((string?)null).ToList();

        // Assert
        Assert.Empty(filtered);
    }

    [Fact, Priority(42)]
    public void FilterByTenant_WithTenantContext_ShouldFilter()
    {
        // Arrange
        var entities = new List<TenantEntity>
        {
            new() { Id = 1, Name = "Entity 1", TenantId = "tenant-1" },
            new() { Id = 2, Name = "Entity 2", TenantId = "tenant-2" }
        }.AsQueryable();
        var context = TenantContext.FromId("tenant-1");

        // Act
        var filtered = entities.FilterByTenant(context).ToList();

        // Assert
        Assert.Single(filtered);
        Assert.Equal("tenant-1", filtered[0].TenantId);
    }

    [Fact, Priority(43)]
    public void WithTenant_ShouldSetTenantId()
    {
        // Arrange
        var entity = new TenantEntity { Id = 1, Name = "Entity 1" };

        // Act
        entity.WithTenant("tenant-123");

        // Assert
        Assert.Equal("tenant-123", entity.TenantId);
    }

    [Fact, Priority(44)]
    public void WithTenant_ShouldThrowWhenTenantIdEmpty()
    {
        // Arrange
        var entity = new TenantEntity { Id = 1, Name = "Entity 1" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => entity.WithTenant(""));
    }

    #endregion

    #region [ CurrentUser Tests ]

    [Fact, Priority(50)]
    public void CurrentUser_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var roles = new[] { "Admin", "User" };
        var claims = new Dictionary<string, string?> { { "Department", "IT" }, { "Level", "Senior" } };
        var user = new CurrentUser(
            id: "user-123",
            name: "John Doe",
            email: "john@example.com",
            isAuthenticated: true,
            roles: roles,
            claims: claims);

        // Assert
        Assert.Equal("user-123", user.Id);
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("john@example.com", user.Email);
        Assert.True(user.IsAuthenticated);
        Assert.Contains("Admin", user.Roles);
        Assert.Contains("User", user.Roles);
        Assert.Equal("IT", user.GetClaim("Department"));
    }

    [Fact, Priority(51)]
    public void CurrentUser_Anonymous_ShouldNotBeAuthenticated()
    {
        // Arrange
        var user = CurrentUser.Anonymous;

        // Assert
        Assert.Null(user.Id);
        Assert.False(user.IsAuthenticated);
    }

    [Fact, Priority(52)]
    public void CurrentUser_IsInRole_ShouldCheckRoleCaseInsensitive()
    {
        // Arrange
        var user = CurrentUser.Create("user-1", "John", roles: new[] { "Admin", "User" });

        // Assert
        Assert.True(user.IsInRole("admin"));
        Assert.True(user.IsInRole("ADMIN"));
        Assert.True(user.IsInRole("Admin"));
        Assert.False(user.IsInRole("SuperAdmin"));
    }

    [Fact, Priority(53)]
    public void CurrentUserAccessor_ShouldStoreAndRetrieveUser()
    {
        // Arrange
        var accessor = new CurrentUserAccessor();
        var user = CurrentUser.Create("user-1", "John Doe");

        // Act
        accessor.User = user;

        // Assert
        Assert.NotNull(accessor.User);
        Assert.Equal("user-1", accessor.User.Id);
    }

    #endregion

    #region [ TenantBehavior Tests ]

    [Fact, Priority(60)]
    public async Task TenantBehavior_ShouldResolveTenantFromResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantResolver, FixedTenantResolver>();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TenantTestCommand).Assembly);
            options.RegisterTenantBehavior = true;
        });
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var accessor = sp.GetRequiredService<ITenantContextAccessor>();
        var command = new TenantTestCommand { Name = "Test" };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("fixed-tenant", result);
    }

    [Fact, Priority(61)]
    public async Task TenantBehavior_WithTenantRequired_ShouldThrowWhenNoTenant()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        // No tenant resolver registered
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TenantRequiredTestCommand).Assembly);
            options.RegisterTenantBehavior = true;
        });
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new TenantRequiredTestCommand { Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<TenantRequiredException>(() => mediator.SendAsync(command));
    }

    [Fact, Priority(62)]
    public async Task TenantBehavior_WithTenantAware_ShouldUseOverrideTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddOrUpdate(new TenantContext("override-tenant", "Override Tenant"));
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<ITenantStore>(store);
        services.AddScoped<ITenantResolver, FixedTenantResolver>(); // Returns "fixed-tenant"
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TenantAwareTestCommand).Assembly);
            options.RegisterTenantBehavior = true;
        });
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new TenantAwareTestCommand 
        { 
            Name = "Test", 
            TenantOverride = "override-tenant"  // Override the resolved tenant
        };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("override-tenant", result);
    }

    #endregion

    #region [ CurrentUserBehavior Tests ]

    [Fact, Priority(70)]
    public async Task CurrentUserBehavior_ShouldResolveUserFromFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<ICurrentUserFactory, FixedCurrentUserFactory>();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(UserTestCommand).Assembly);
            options.RegisterCurrentUserBehavior = true;
        });
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new UserTestCommand();

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("fixed-user-123", result);
    }

    [Fact, Priority(71)]
    public async Task CurrentUserBehavior_WithUserRequired_ShouldThrowWhenNoUser()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        // No user factory registered
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(UserRequiredTestCommand).Assembly);
            options.RegisterCurrentUserBehavior = true;
        });
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new UserRequiredTestCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => mediator.SendAsync(command));
    }

    #endregion

    #region [ WithMultiTenancy Configuration Test ]

    [Fact, Priority(80)]
    public void WithMultiTenancy_ShouldRegisterBothBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithMultiTenancy();
        });
        var sp = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(sp.GetService<ITenantContextAccessor>());
        Assert.NotNull(sp.GetService<ICurrentUserAccessor>());
        Assert.NotNull(sp.GetService<ITenantFilter>());
    }

    #endregion
}

#region [ Test Support Classes ]

/// <summary>
/// Test entity with tenant support.
/// </summary>
internal class TenantEntity : IHasTenant
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string TenantId { get; set; } = default!;
}

/// <summary>
/// Fixed tenant resolver for testing.
/// </summary>
internal class FixedTenantResolver : ITenantResolver
{
    public Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ITenantContext?>(TenantContext.FromId("fixed-tenant"));
    }
}

/// <summary>
/// Fixed current user factory for testing.
/// </summary>
internal class FixedCurrentUserFactory : ICurrentUserFactory
{
    public ICurrentUser? CreateFromCurrentContext()
    {
        return CurrentUser.Create("fixed-user-123", "Fixed User");
    }
}

/// <summary>
/// Test command that uses tenant context.
/// </summary>
internal record TenantTestCommand : IMediatorCommand<string>
{
    public string Name { get; init; } = default!;
}

internal class TenantTestCommandHandler : IMediatorCommandHandler<TenantTestCommand, string>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantTestCommandHandler(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Task<string> Handle(TenantTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tenantContextAccessor.Context?.TenantId ?? "no-tenant");
    }
}

/// <summary>
/// Test command that requires a tenant.
/// </summary>
internal record TenantRequiredTestCommand : IMediatorCommand<string>, ITenantRequired
{
    public string Name { get; init; } = default!;
}

internal class TenantRequiredTestCommandHandler : IMediatorCommandHandler<TenantRequiredTestCommand, string>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantRequiredTestCommandHandler(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Task<string> Handle(TenantRequiredTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tenantContextAccessor.Context?.TenantId ?? "no-tenant");
    }
}

/// <summary>
/// Test command that is tenant-aware with override support.
/// </summary>
internal record TenantAwareTestCommand : IMediatorCommand<string>, ITenantAware
{
    public string Name { get; init; } = default!;
    public string? TenantOverride { get; init; }
    
    public string? OverrideTenantId => TenantOverride;
}

internal class TenantAwareTestCommandHandler : IMediatorCommandHandler<TenantAwareTestCommand, string>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantAwareTestCommandHandler(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Task<string> Handle(TenantAwareTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tenantContextAccessor.Context?.TenantId ?? "no-tenant");
    }
}

/// <summary>
/// Test command that uses current user context.
/// </summary>
internal record UserTestCommand : IMediatorCommand<string>;

internal class UserTestCommandHandler : IMediatorCommandHandler<UserTestCommand, string>
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public UserTestCommandHandler(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public Task<string> Handle(UserTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_currentUserAccessor.User?.Id ?? "no-user");
    }
}

/// <summary>
/// Test command that requires an authenticated user.
/// </summary>
internal record UserRequiredTestCommand : IMediatorCommand<string>, IUserRequired;

internal class UserRequiredTestCommandHandler : IMediatorCommandHandler<UserRequiredTestCommand, string>
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public UserRequiredTestCommandHandler(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public Task<string> Handle(UserRequiredTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_currentUserAccessor.User?.Id ?? "no-user");
    }
}

#endregion

