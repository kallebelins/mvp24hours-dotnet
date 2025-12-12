//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Example strongly-typed ID for Customer entities.
    /// </summary>
    /// <example>
    /// <code>
    /// var customerId = CustomerId.New();
    /// var customer = new Customer { Id = customerId };
    /// 
    /// // Type safety - won't compile:
    /// // OrderId orderId = customerId; // Error!
    /// </code>
    /// </example>
    public sealed class CustomerId : GuidEntityId<CustomerId>
    {
        /// <summary>
        /// Creates a CustomerId from an existing Guid value.
        /// </summary>
        public CustomerId(Guid value) : base(value) { }

        /// <summary>
        /// Creates a new CustomerId with a new Guid.
        /// </summary>
        public static CustomerId New() => new(Guid.NewGuid());

        /// <summary>
        /// Gets an empty CustomerId.
        /// </summary>
        public static CustomerId Empty => new(Guid.Empty);

        /// <summary>
        /// Tries to parse a string into a CustomerId.
        /// </summary>
        public static bool TryParse(string value, out CustomerId result)
        {
            result = null!;
            if (Guid.TryParse(value, out var guid))
            {
                result = new CustomerId(guid);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Example strongly-typed ID for Order entities.
    /// </summary>
    public sealed class OrderId : GuidEntityId<OrderId>
    {
        /// <summary>
        /// Creates an OrderId from an existing Guid value.
        /// </summary>
        public OrderId(Guid value) : base(value) { }

        /// <summary>
        /// Creates a new OrderId with a new Guid.
        /// </summary>
        public static OrderId New() => new(Guid.NewGuid());

        /// <summary>
        /// Gets an empty OrderId.
        /// </summary>
        public static OrderId Empty => new(Guid.Empty);

        /// <summary>
        /// Tries to parse a string into an OrderId.
        /// </summary>
        public static bool TryParse(string value, out OrderId result)
        {
            result = null!;
            if (Guid.TryParse(value, out var guid))
            {
                result = new OrderId(guid);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Example strongly-typed ID for Product entities.
    /// </summary>
    public sealed class ProductId : GuidEntityId<ProductId>
    {
        /// <summary>
        /// Creates a ProductId from an existing Guid value.
        /// </summary>
        public ProductId(Guid value) : base(value) { }

        /// <summary>
        /// Creates a new ProductId with a new Guid.
        /// </summary>
        public static ProductId New() => new(Guid.NewGuid());

        /// <summary>
        /// Gets an empty ProductId.
        /// </summary>
        public static ProductId Empty => new(Guid.Empty);

        /// <summary>
        /// Tries to parse a string into a ProductId.
        /// </summary>
        public static bool TryParse(string value, out ProductId result)
        {
            result = null!;
            if (Guid.TryParse(value, out var guid))
            {
                result = new ProductId(guid);
                return true;
            }
            return false;
        }
    }
}

