//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

/// <summary>
/// Default implementation of <see cref="ITenantContext"/>.
/// </summary>
/// <remarks>
/// This is an immutable record that contains all context information for a tenant.
/// </remarks>
public sealed record TenantContext : ITenantContext
{
    /// <summary>
    /// Creates a new tenant context with the specified values.
    /// </summary>
    public TenantContext(
        string? tenantId = null,
        string? tenantName = null,
        string? connectionString = null,
        string? schema = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        ConnectionString = connectionString;
        Schema = schema;
        Properties = properties ?? new Dictionary<string, object?>();
    }

    /// <inheritdoc />
    public string? TenantId { get; }

    /// <inheritdoc />
    public string? TenantName { get; }

    /// <inheritdoc />
    public string? ConnectionString { get; }

    /// <inheritdoc />
    public string? Schema { get; }

    /// <inheritdoc />
    public bool HasTenant => !string.IsNullOrEmpty(TenantId);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Properties { get; }

    /// <inheritdoc />
    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Creates a new empty context (no tenant).
    /// </summary>
    public static TenantContext Empty => new();

    /// <summary>
    /// Creates a tenant context with just the tenant ID.
    /// </summary>
    public static TenantContext FromId(string tenantId) => new(tenantId: tenantId);

    /// <summary>
    /// Creates a tenant context with ID and name.
    /// </summary>
    public static TenantContext FromIdAndName(string tenantId, string tenantName) => 
        new(tenantId: tenantId, tenantName: tenantName);
}

/// <summary>
/// Default implementation of <see cref="ITenantContextAccessor"/> using AsyncLocal for ambient context.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to maintain the tenant context
/// across async operations within the same logical execution flow.
/// </para>
/// <para>
/// In ASP.NET Core scenarios, this is typically registered as a scoped service and
/// populated by the <see cref="Mvp24Hours.Infrastructure.Cqrs.Behaviors.TenantBehavior{TRequest, TResponse}"/>.
/// </para>
/// </remarks>
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextHolder> _contextHolder = new();

    /// <inheritdoc />
    public ITenantContext? Context
    {
        get => _contextHolder.Value?.Context;
        set
        {
            var holder = _contextHolder.Value;
            if (holder != null)
            {
                holder.Context = null;
            }

            if (value != null)
            {
                _contextHolder.Value = new TenantContextHolder { Context = value };
            }
        }
    }

    private sealed class TenantContextHolder
    {
        public ITenantContext? Context;
    }
}

/// <summary>
/// In-memory implementation of <see cref="ITenantStore"/> for testing and simple scenarios.
/// </summary>
/// <remarks>
/// For production use, implement a database-backed store.
/// </remarks>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly Dictionary<string, TenantContext> _tenantsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TenantContext> _tenantsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Adds or updates a tenant in the store.
    /// </summary>
    /// <param name="tenant">The tenant to add or update.</param>
    public void AddOrUpdate(TenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        
        if (string.IsNullOrEmpty(tenant.TenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenant));
        }

        lock (_lock)
        {
            _tenantsById[tenant.TenantId] = tenant;
            if (!string.IsNullOrEmpty(tenant.TenantName))
            {
                _tenantsByName[tenant.TenantName] = tenant;
            }
        }
    }

    /// <summary>
    /// Removes a tenant from the store.
    /// </summary>
    /// <param name="tenantId">The tenant ID to remove.</param>
    /// <returns>True if the tenant was removed, false if not found.</returns>
    public bool Remove(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return false;
        }

        lock (_lock)
        {
            if (_tenantsById.TryGetValue(tenantId, out var tenant))
            {
                _tenantsById.Remove(tenantId);
                if (!string.IsNullOrEmpty(tenant.TenantName))
                {
                    _tenantsByName.Remove(tenant.TenantName);
                }
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public Task<ITenantContext?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return Task.FromResult<ITenantContext?>(null);
        }

        lock (_lock)
        {
            return Task.FromResult<ITenantContext?>(
                _tenantsById.TryGetValue(tenantId, out var tenant) ? tenant : null);
        }
    }

    /// <inheritdoc />
    public Task<ITenantContext?> GetByNameAsync(string tenantName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantName))
        {
            return Task.FromResult<ITenantContext?>(null);
        }

        lock (_lock)
        {
            return Task.FromResult<ITenantContext?>(
                _tenantsByName.TryGetValue(tenantName, out var tenant) ? tenant : null);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ITenantContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<ITenantContext>>(_tenantsById.Values.ToList());
        }
    }
}

/// <summary>
/// Default implementation of <see cref="ITenantFilter"/>.
/// </summary>
public sealed class TenantFilter : ITenantFilter
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly AsyncLocal<bool> _filterDisabled = new();

    /// <summary>
    /// Creates a new instance of the tenant filter.
    /// </summary>
    /// <param name="tenantContextAccessor">The tenant context accessor.</param>
    public TenantFilter(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public string? CurrentTenantId => _tenantContextAccessor.Context?.TenantId;

    /// <inheritdoc />
    public bool ShouldFilter => !_filterDisabled.Value && !string.IsNullOrEmpty(CurrentTenantId);

    /// <inheritdoc />
    public IDisposable DisableFilter()
    {
        _filterDisabled.Value = true;
        return new FilterDisabler(this);
    }

    private sealed class FilterDisabler : IDisposable
    {
        private readonly TenantFilter _filter;
        private bool _disposed;

        public FilterDisabler(TenantFilter filter)
        {
            _filter = filter;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _filter._filterDisabled.Value = false;
                _disposed = true;
            }
        }
    }
}

