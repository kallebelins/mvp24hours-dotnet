//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure.DependencyInjection;

/// <summary>
/// Base marker interface for dependency injection service lifetime conventions.
/// Classes implementing derived interfaces will be automatically registered
/// with the corresponding lifetime when using convention-based registration.
/// </summary>
/// <remarks>
/// <para>
/// This follows the convention-over-configuration principle, allowing services
/// to declare their intended DI lifetime through interface implementation.
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// // Service will be registered as Scoped
/// public class CustomerService : ICustomerService, IScopedService { }
/// 
/// // Service will be registered as Singleton  
/// public class CacheService : ICacheService, ISingletonService { }
/// 
/// // Service will be registered as Transient
/// public class EmailService : IEmailService, ITransientService { }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IScopedService"/>
/// <seealso cref="ISingletonService"/>
/// <seealso cref="ITransientService"/>
public interface IServiceLifetimeMarker
{
}

/// <summary>
/// Marker interface for services that should be registered with Scoped lifetime.
/// One instance is created per HTTP request or scope.
/// </summary>
/// <remarks>
/// <para>
/// Scoped services are ideal for:
/// <list type="bullet">
/// <item>Database contexts and repositories</item>
/// <item>Unit of Work implementations</item>
/// <item>Services that maintain state per-request</item>
/// <item>Services that depend on scoped resources</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public interface IOrderService : IScopedService
/// {
///     Task&lt;Order&gt; GetByIdAsync(Guid id);
/// }
/// 
/// public class OrderService : IOrderService
/// {
///     private readonly IUnitOfWorkAsync _unitOfWork;
///     
///     public OrderService(IUnitOfWorkAsync unitOfWork)
///     {
///         _unitOfWork = unitOfWork;
///     }
///     
///     public async Task&lt;Order&gt; GetByIdAsync(Guid id)
///     {
///         // Implementation
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="ISingletonService"/>
/// <seealso cref="ITransientService"/>
public interface IScopedService : IServiceLifetimeMarker
{
}

/// <summary>
/// Marker interface for services that should be registered with Singleton lifetime.
/// One instance is shared across all requests and the entire application lifetime.
/// </summary>
/// <remarks>
/// <para>
/// Singleton services are ideal for:
/// <list type="bullet">
/// <item>Configuration services</item>
/// <item>Caching services</item>
/// <item>Logging services</item>
/// <item>Thread-safe stateless services</item>
/// <item>Services that are expensive to create</item>
/// </list>
/// </para>
/// <para>
/// <strong>Warning:</strong> Singleton services must be thread-safe since they can be
/// accessed from multiple threads simultaneously.
/// </para>
/// <para>
/// <strong>Warning:</strong> Singleton services cannot depend on Scoped or Transient services
/// as this would cause a captive dependency problem.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public interface ICacheManager : ISingletonService
/// {
///     T? Get&lt;T&gt;(string key);
///     void Set&lt;T&gt;(string key, T value, TimeSpan expiration);
/// }
/// 
/// public class CacheManager : ICacheManager
/// {
///     private readonly ConcurrentDictionary&lt;string, object&gt; _cache = new();
///     
///     public T? Get&lt;T&gt;(string key) { /* ... */ }
///     public void Set&lt;T&gt;(string key, T value, TimeSpan expiration) { /* ... */ }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IScopedService"/>
/// <seealso cref="ITransientService"/>
public interface ISingletonService : IServiceLifetimeMarker
{
}

/// <summary>
/// Marker interface for services that should be registered with Transient lifetime.
/// A new instance is created each time the service is requested.
/// </summary>
/// <remarks>
/// <para>
/// Transient services are ideal for:
/// <list type="bullet">
/// <item>Lightweight, stateless services</item>
/// <item>Services that need fresh state for each use</item>
/// <item>Factory-like services</item>
/// <item>Services with disposable resources</item>
/// </list>
/// </para>
/// <para>
/// <strong>Note:</strong> Transient services can be injected into Scoped and Singleton services,
/// but doing so may affect the expected transient behavior.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public interface IEmailBuilder : ITransientService
/// {
///     IEmailBuilder WithSubject(string subject);
///     IEmailBuilder WithBody(string body);
///     IEmailBuilder AddRecipient(string email);
///     EmailMessage Build();
/// }
/// 
/// public class EmailBuilder : IEmailBuilder
/// {
///     private string _subject = string.Empty;
///     private string _body = string.Empty;
///     private readonly List&lt;string&gt; _recipients = new();
///     
///     public IEmailBuilder WithSubject(string subject) { _subject = subject; return this; }
///     public IEmailBuilder WithBody(string body) { _body = body; return this; }
///     public IEmailBuilder AddRecipient(string email) { _recipients.Add(email); return this; }
///     public EmailMessage Build() => new(_subject, _body, _recipients);
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IScopedService"/>
/// <seealso cref="ISingletonService"/>
public interface ITransientService : IServiceLifetimeMarker
{
}

/// <summary>
/// Marker interface for self-registering services.
/// Services implementing this interface will be registered by their concrete type as well.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when you want a service to be resolvable by its concrete type
/// in addition to its interface type.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public class PaymentProcessor : IPaymentProcessor, IScopedService, ISelfRegistering
/// {
///     // Can be resolved as IPaymentProcessor or PaymentProcessor
/// }
/// </code>
/// </para>
/// </remarks>
public interface ISelfRegistering : IServiceLifetimeMarker
{
}

/// <summary>
/// Marker interface for services that should be keyed/named when registered.
/// Use with <see cref="ServiceKeyAttribute"/> to specify the key.
/// </summary>
/// <remarks>
/// <para>
/// .NET 8 introduced Keyed Services for scenarios where you need multiple implementations
/// of the same interface with different behaviors.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// [ServiceKey("primary")]
/// public class PrimaryDatabase : IDatabase, IScopedService, IKeyedService { }
/// 
/// [ServiceKey("secondary")]
/// public class SecondaryDatabase : IDatabase, IScopedService, IKeyedService { }
/// 
/// // Resolve with key
/// var primary = serviceProvider.GetRequiredKeyedService&lt;IDatabase&gt;("primary");
/// </code>
/// </para>
/// </remarks>
public interface IKeyedService : IServiceLifetimeMarker
{
}

