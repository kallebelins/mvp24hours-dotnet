//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Models
{
    /// <summary>
    /// Default implementation of <see cref="IParentJob"/>.
    /// </summary>
    /// <remarks>
    /// This class tracks parent-child relationships between jobs and provides
    /// methods to manage child jobs.
    /// </remarks>
    public class ParentJob : IParentJob
    {
        private readonly List<string> _childJobIds = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentJob"/> class.
        /// </summary>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="options">Parent-child job options.</param>
        public ParentJob(string parentJobId, ParentChildJobOptions? options = null)
        {
            ParentJobId = parentJobId ?? throw new ArgumentNullException(nameof(parentJobId));
            Options = options ?? new ParentChildJobOptions();
        }

        /// <inheritdoc />
        public string ParentJobId { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> ChildJobIds => _childJobIds.AsReadOnly();

        /// <inheritdoc />
        public ParentChildJobOptions Options { get; }

        /// <summary>
        /// Adds a child job ID to this parent job.
        /// </summary>
        /// <param name="childJobId">The child job ID to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when childJobId is null or empty.</exception>
        public void AddChild(string childJobId)
        {
            if (string.IsNullOrWhiteSpace(childJobId))
            {
                throw new ArgumentException("Child job ID cannot be null or empty.", nameof(childJobId));
            }

            if (!_childJobIds.Contains(childJobId))
            {
                _childJobIds.Add(childJobId);
            }
        }

        /// <summary>
        /// Removes a child job ID from this parent job.
        /// </summary>
        /// <param name="childJobId">The child job ID to remove.</param>
        /// <returns><c>true</c> if the child was removed; <c>false</c> if the child was not found.</returns>
        public bool RemoveChild(string childJobId)
        {
            return _childJobIds.Remove(childJobId);
        }

        /// <summary>
        /// Clears all child job IDs.
        /// </summary>
        public void ClearChildren()
        {
            _childJobIds.Clear();
        }
    }

    /// <summary>
    /// Default implementation of <see cref="IChildJob"/>.
    /// </summary>
    public class ChildJob : IChildJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildJob"/> class.
        /// </summary>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="childJobId">The ID of the child job.</param>
        /// <param name="executionOrder">Optional execution order for sequential execution.</param>
        /// <param name="siblingDependencies">IDs of sibling jobs that must complete before this child executes.</param>
        public ChildJob(
            string parentJobId,
            string childJobId,
            int? executionOrder = null,
            IEnumerable<string>? siblingDependencies = null)
        {
            ParentJobId = parentJobId ?? throw new ArgumentNullException(nameof(parentJobId));
            ChildJobId = childJobId ?? throw new ArgumentNullException(nameof(childJobId));
            ExecutionOrder = executionOrder;
            SiblingDependencies = siblingDependencies?.ToList() ?? new List<string>();
        }

        /// <inheritdoc />
        public string ParentJobId { get; }

        /// <inheritdoc />
        public string ChildJobId { get; }

        /// <inheritdoc />
        public int? ExecutionOrder { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> SiblingDependencies { get; }
    }
}

