//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Validation
{
    /// <summary>
    /// Interface for cascade (nested) validation of complex objects.
    /// Validates parent object and recursively validates all nested child objects.
    /// </summary>
    /// <typeparam name="T">The root type to validate.</typeparam>
    /// <remarks>
    /// <para>
    /// Cascade validation is useful when you have DTOs with nested objects that 
    /// also need to be validated.
    /// </para>
    /// <para>
    /// <strong>Example:</strong>
    /// <code>
    /// public class OrderDto
    /// {
    ///     public string CustomerName { get; set; }
    ///     public AddressDto ShippingAddress { get; set; } // Nested object
    ///     public List&lt;OrderItemDto&gt; Items { get; set; } // Nested collection
    /// }
    /// 
    /// // Cascade validator will validate OrderDto, AddressDto, and all OrderItemDto instances
    /// var result = await cascadeValidator.ValidateWithNestedAsync(order);
    /// </code>
    /// </para>
    /// </remarks>
    public interface ICascadeValidator<T> where T : class
    {
        /// <summary>
        /// Validates the object including all nested objects.
        /// </summary>
        /// <param name="instance">The root instance to validate.</param>
        /// <returns>Validation result with errors from all levels.</returns>
        ValidationServiceResult ValidateWithNested(T instance);

        /// <summary>
        /// Validates the object including all nested objects with options.
        /// </summary>
        /// <param name="instance">The root instance to validate.</param>
        /// <param name="options">Validation options.</param>
        /// <returns>Validation result with errors from all levels.</returns>
        ValidationServiceResult ValidateWithNested(T instance, ValidationOptions options);

        /// <summary>
        /// Validates the object including all nested objects asynchronously.
        /// </summary>
        /// <param name="instance">The root instance to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with errors from all levels.</returns>
        Task<ValidationServiceResult> ValidateWithNestedAsync(T instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the object including all nested objects asynchronously with options.
        /// </summary>
        /// <param name="instance">The root instance to validate.</param>
        /// <param name="options">Validation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with errors from all levels.</returns>
        Task<ValidationServiceResult> ValidateWithNestedAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Marker interface for objects that should have cascade validation applied.
    /// Apply this to DTOs that contain nested objects requiring validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this interface is implemented, the validation pipeline will automatically
    /// apply cascade validation to nested properties.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// <code>
    /// public class OrderDto : IHasNestedValidation
    /// {
    ///     public AddressDto Address { get; set; }
    ///     public List&lt;OrderItemDto&gt; Items { get; set; }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IHasNestedValidation
    {
    }

    /// <summary>
    /// Attribute to mark properties that should be validated as nested objects.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ValidateNestedAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets whether to skip validation if the property value is null.
        /// Default is true.
        /// </summary>
        public bool SkipIfNull { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for nested validation on this property.
        /// Default is 5.
        /// </summary>
        public int MaxDepth { get; set; } = 5;
    }

    /// <summary>
    /// Represents an error from nested validation with full property path.
    /// </summary>
    public class NestedValidationError
    {
        /// <summary>
        /// Gets or sets the full property path (e.g., "Order.Items[0].ProductId").
        /// </summary>
        public string PropertyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validation depth level where the error occurred.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the type name of the object that failed validation.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Converts to IMessageResult.
        /// </summary>
        public IMessageResult ToMessageResult()
        {
            return new Core.ValueObjects.Logic.MessageResult(
                PropertyPath,
                ErrorMessage,
                Core.Enums.MessageType.Error);
        }
    }
}

