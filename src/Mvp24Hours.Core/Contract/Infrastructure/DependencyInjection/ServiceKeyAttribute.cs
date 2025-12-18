//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.DependencyInjection;

/// <summary>
/// Specifies the key to use when registering a keyed service.
/// Use in conjunction with <see cref="IKeyedService"/> marker interface.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by convention-based registration to determine the key
/// when registering keyed services. It leverages .NET 8's built-in keyed services feature.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // Define multiple implementations with different keys
/// [ServiceKey("stripe")]
/// public class StripePaymentService : IPaymentService, IScopedService, IKeyedService { }
/// 
/// [ServiceKey("paypal")]
/// public class PayPalPaymentService : IPaymentService, IScopedService, IKeyedService { }
/// 
/// // Resolve by key
/// public class OrderController
/// {
///     public OrderController(
///         [FromKeyedServices("stripe")] IPaymentService stripePayment,
///         [FromKeyedServices("paypal")] IPaymentService paypalPayment)
///     {
///         // Use appropriate payment service based on business logic
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IKeyedService"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceKeyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceKeyAttribute"/> class.
    /// </summary>
    /// <param name="key">The key to use when registering the service.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when key is empty or whitespace.</exception>
    public ServiceKeyAttribute(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Service key cannot be empty or whitespace.", nameof(key));
        }

        Key = key;
    }

    /// <summary>
    /// Gets the key used for registering the service.
    /// </summary>
    public string Key { get; }
}

/// <summary>
/// Specifies the order in which the service should be registered.
/// Lower values are registered first.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when the order of service registration matters,
/// such as when services are resolved as <see cref="IEnumerable{T}"/>.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// [ServiceOrder(1)]
/// public class PrimaryValidator : IValidator, IScopedService { }
/// 
/// [ServiceOrder(2)]
/// public class SecondaryValidator : IValidator, IScopedService { }
/// 
/// // When resolved as IEnumerable&lt;IValidator&gt;, order is preserved
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceOrderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">The registration order. Lower values are registered first.</param>
    public ServiceOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the registration order.
    /// </summary>
    public int Order { get; }
}

/// <summary>
/// Specifies that the service should replace any existing registration for the same interface.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when you want a service to override an existing registration,
/// such as replacing a default implementation with a custom one.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // Default implementation
/// public class DefaultLogger : ILogger, IScopedService { }
/// 
/// // Custom implementation that replaces the default
/// [ServiceReplace]
/// public class CustomLogger : ILogger, IScopedService { }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceReplaceAttribute : Attribute
{
}

/// <summary>
/// Specifies that the service should only be registered if no other implementation exists.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute for default implementations that can be overridden by
/// more specific implementations.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // Default implementation - only registered if ICache is not already registered
/// [ServiceTryAdd]
/// public class MemoryCache : ICache, ISingletonService { }
/// 
/// // If this is registered first, MemoryCache will not be registered
/// public class RedisCache : ICache, ISingletonService { }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceTryAddAttribute : Attribute
{
}

/// <summary>
/// Excludes a class from convention-based service registration.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when a class implements a marker interface but should not
/// be registered automatically.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // This class will not be registered even though it implements IScopedService
/// [ServiceIgnore]
/// public class TestService : IService, IScopedService { }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceIgnoreAttribute : Attribute
{
}

