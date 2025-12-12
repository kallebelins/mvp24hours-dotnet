//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Abstraction for GUID generation, enabling testability and custom generation strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Using IGuidGenerator instead of Guid.NewGuid() directly allows you to:
    /// - Write deterministic tests with predictable GUIDs
    /// - Use sequential GUIDs for better database performance
    /// - Use different GUID generation strategies
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderService
    /// {
    ///     private readonly IGuidGenerator _guidGenerator;
    ///     
    ///     public OrderService(IGuidGenerator guidGenerator)
    ///     {
    ///         _guidGenerator = guidGenerator;
    ///     }
    ///     
    ///     public Order CreateOrder()
    ///     {
    ///         return new Order { Id = _guidGenerator.NewGuid() };
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IGuidGenerator
    {
        /// <summary>
        /// Generates a new GUID.
        /// </summary>
        /// <returns>A new unique identifier.</returns>
        Guid NewGuid();
    }
}

