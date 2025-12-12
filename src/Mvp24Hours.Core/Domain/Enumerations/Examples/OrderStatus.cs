//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Core.Domain.Enumerations.Examples
{
    /// <summary>
    /// Example Smart Enum for order status with associated behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This example demonstrates how Smart Enums can encapsulate domain logic
    /// related to each status, such as which transitions are allowed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var order = new Order { Status = OrderStatus.Pending };
    /// 
    /// if (order.Status.CanTransitionTo(OrderStatus.Processing))
    /// {
    ///     order.Status = OrderStatus.Processing;
    /// }
    /// 
    /// if (order.Status.CanCancel)
    /// {
    ///     order.Status = OrderStatus.Cancelled;
    /// }
    /// </code>
    /// </example>
    public sealed class OrderStatus : Enumeration<OrderStatus>
    {
        /// <summary>
        /// Order has been created but not yet processed.
        /// </summary>
        public static readonly OrderStatus Pending = new(1, nameof(Pending), canCancel: true);

        /// <summary>
        /// Order is being processed.
        /// </summary>
        public static readonly OrderStatus Processing = new(2, nameof(Processing), canCancel: true);

        /// <summary>
        /// Order has been shipped.
        /// </summary>
        public static readonly OrderStatus Shipped = new(3, nameof(Shipped), canCancel: false);

        /// <summary>
        /// Order has been delivered to the customer.
        /// </summary>
        public static readonly OrderStatus Delivered = new(4, nameof(Delivered), canCancel: false);

        /// <summary>
        /// Order has been cancelled.
        /// </summary>
        public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled), canCancel: false);

        /// <summary>
        /// Order has been refunded.
        /// </summary>
        public static readonly OrderStatus Refunded = new(6, nameof(Refunded), canCancel: false);

        /// <summary>
        /// Gets a value indicating whether the order can be cancelled in this status.
        /// </summary>
        public bool CanCancel { get; }

        /// <summary>
        /// Gets a value indicating whether this is a terminal status (no further transitions).
        /// </summary>
        public bool IsTerminal => this == Delivered || this == Cancelled || this == Refunded;

        private OrderStatus(int value, string name, bool canCancel) : base(value, name)
        {
            CanCancel = canCancel;
        }

        /// <summary>
        /// Checks if the order can transition from this status to the specified status.
        /// </summary>
        /// <param name="newStatus">The target status.</param>
        /// <returns>True if the transition is allowed; otherwise, false.</returns>
        public bool CanTransitionTo(OrderStatus newStatus)
        {
            if (newStatus == null) return false;
            if (this == newStatus) return false;
            if (IsTerminal) return false;

            // Define valid transitions
            return (this, newStatus) switch
            {
                (_, _) when newStatus == Cancelled && CanCancel => true,
                var (from, to) when from == Pending && to == Processing => true,
                var (from, to) when from == Processing && to == Shipped => true,
                var (from, to) when from == Shipped && to == Delivered => true,
                var (from, to) when from == Delivered && to == Refunded => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets all active (non-terminal) statuses.
        /// </summary>
        public static IEnumerable<OrderStatus> ActiveStatuses =>
            GetAll().Where(s => !s.IsTerminal);
    }
}

