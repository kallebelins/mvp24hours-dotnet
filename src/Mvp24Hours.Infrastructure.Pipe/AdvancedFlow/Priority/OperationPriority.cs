//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Priority
{
    /// <summary>
    /// Defines standard priority levels for pipeline operations.
    /// </summary>
    public enum PriorityLevel
    {
        /// <summary>
        /// Lowest priority. Executed last.
        /// </summary>
        Lowest = 0,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 25,

        /// <summary>
        /// Normal/default priority.
        /// </summary>
        Normal = 50,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 75,

        /// <summary>
        /// Highest priority. Executed first.
        /// </summary>
        Highest = 100,

        /// <summary>
        /// Critical operations that must run before anything else.
        /// </summary>
        Critical = 150
    }

    /// <summary>
    /// Attribute to specify operation priority for automatic ordering.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class OperationPriorityAttribute : Attribute
    {
        /// <summary>
        /// Gets the priority value.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets the priority level.
        /// </summary>
        public PriorityLevel Level { get; }

        /// <summary>
        /// Gets optional grouping name for operations that should run together.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Creates a priority attribute with a specific priority value.
        /// </summary>
        /// <param name="priority">The priority value (higher = executes first).</param>
        public OperationPriorityAttribute(int priority)
        {
            Priority = priority;
            Level = priority switch
            {
                >= 150 => PriorityLevel.Critical,
                >= 75 => PriorityLevel.High,
                >= 50 => PriorityLevel.Normal,
                >= 25 => PriorityLevel.Low,
                _ => PriorityLevel.Lowest
            };
        }

        /// <summary>
        /// Creates a priority attribute with a priority level.
        /// </summary>
        /// <param name="level">The priority level.</param>
        public OperationPriorityAttribute(PriorityLevel level)
        {
            Level = level;
            Priority = (int)level;
        }
    }

    /// <summary>
    /// Interface for operations that declare their own priority.
    /// </summary>
    public interface IPrioritizedOperation
    {
        /// <summary>
        /// Gets the priority of this operation. Higher values execute first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the optional group this operation belongs to.
        /// </summary>
        string? Group { get; }
    }

    /// <summary>
    /// Wraps an operation with priority information.
    /// </summary>
    /// <typeparam name="T">The operation type.</typeparam>
    public sealed class PrioritizedOperation<T> : IPrioritizedOperation where T : class
    {
        /// <summary>
        /// Gets the underlying operation.
        /// </summary>
        public T Operation { get; }

        /// <inheritdoc/>
        public int Priority { get; }

        /// <inheritdoc/>
        public string? Group { get; }

        /// <summary>
        /// Creates a prioritized operation wrapper.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="priority">The priority value.</param>
        /// <param name="group">Optional group name.</param>
        public PrioritizedOperation(T operation, int priority, string? group = null)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Priority = priority;
            Group = group;
        }

        /// <summary>
        /// Creates a prioritized operation wrapper with a priority level.
        /// </summary>
        public PrioritizedOperation(T operation, PriorityLevel level, string? group = null)
            : this(operation, (int)level, group)
        {
        }
    }

    /// <summary>
    /// Comparer for sorting operations by priority (highest first).
    /// </summary>
    public sealed class OperationPriorityComparer : IComparer<IPrioritizedOperation>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly OperationPriorityComparer Instance = new();

        private OperationPriorityComparer() { }

        /// <inheritdoc/>
        public int Compare(IPrioritizedOperation? x, IPrioritizedOperation? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            // Higher priority comes first (descending order)
            return y.Priority.CompareTo(x.Priority);
        }
    }

    /// <summary>
    /// Helper for extracting priority information from operations.
    /// </summary>
    public static class OperationPriorityHelper
    {
        /// <summary>
        /// Gets the priority of an operation.
        /// </summary>
        /// <param name="operation">The operation to check.</param>
        /// <returns>The priority value, or 50 (Normal) if not specified.</returns>
        public static int GetPriority(object operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Check if operation implements IPrioritizedOperation
            if (operation is IPrioritizedOperation prioritized)
            {
                return prioritized.Priority;
            }

            // Check for OperationPriorityAttribute
            var attribute = (OperationPriorityAttribute?)Attribute.GetCustomAttribute(
                operation.GetType(),
                typeof(OperationPriorityAttribute));

            return attribute?.Priority ?? (int)PriorityLevel.Normal;
        }

        /// <summary>
        /// Gets the group of an operation.
        /// </summary>
        /// <param name="operation">The operation to check.</param>
        /// <returns>The group name, or null if not specified.</returns>
        public static string? GetGroup(object operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Check if operation implements IPrioritizedOperation
            if (operation is IPrioritizedOperation prioritized)
            {
                return prioritized.Group;
            }

            // Check for OperationPriorityAttribute
            var attribute = (OperationPriorityAttribute?)Attribute.GetCustomAttribute(
                operation.GetType(),
                typeof(OperationPriorityAttribute));

            return attribute?.Group;
        }

        /// <summary>
        /// Sorts operations by priority (highest first).
        /// </summary>
        /// <typeparam name="T">The operation type.</typeparam>
        /// <param name="operations">The operations to sort.</param>
        /// <returns>Operations sorted by priority.</returns>
        public static IEnumerable<T> SortByPriority<T>(IEnumerable<T> operations)
        {
            return operations
                .Select(op => (Operation: op, Priority: GetPriority(op!)))
                .OrderByDescending(x => x.Priority)
                .Select(x => x.Operation);
        }

        /// <summary>
        /// Groups operations by their group name and sorts by priority within each group.
        /// </summary>
        /// <typeparam name="T">The operation type.</typeparam>
        /// <param name="operations">The operations to group and sort.</param>
        /// <returns>Grouped and sorted operations.</returns>
        public static IEnumerable<IGrouping<string?, T>> GroupAndSortByPriority<T>(IEnumerable<T> operations)
        {
            return operations
                .Select(op => (Operation: op, Priority: GetPriority(op!), Group: GetGroup(op!)))
                .GroupBy(x => x.Group)
                .OrderByDescending(g => g.Max(x => x.Priority))
                .Select(g => new PriorityGrouping<T>(
                    g.Key,
                    g.OrderByDescending(x => x.Priority).Select(x => x.Operation)));
        }
    }

    /// <summary>
    /// Represents a group of operations with a common group name.
    /// </summary>
    internal sealed class PriorityGrouping<T> : IGrouping<string?, T>
    {
        private readonly IEnumerable<T> _items;

        public PriorityGrouping(string? key, IEnumerable<T> items)
        {
            Key = key;
            _items = items;
        }

        public string? Key { get; }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

