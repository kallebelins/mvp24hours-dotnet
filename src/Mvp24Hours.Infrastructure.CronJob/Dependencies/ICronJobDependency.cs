//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Dependencies
{
    /// <summary>
    /// Represents a dependency relationship between CronJobs.
    /// A job can depend on one or more other jobs to complete before it can execute.
    /// </summary>
    public interface ICronJobDependency
    {
        /// <summary>
        /// Gets the name of the dependent job (the job that has dependencies).
        /// </summary>
        string DependentJobName { get; }

        /// <summary>
        /// Gets the names of the jobs that must complete before the dependent job can run.
        /// </summary>
        IReadOnlyList<string> RequiredJobNames { get; }

        /// <summary>
        /// Gets whether all required jobs must succeed for the dependency to be satisfied.
        /// If false, completion (even with failure) is sufficient.
        /// </summary>
        bool RequireSuccess { get; }

        /// <summary>
        /// Gets the maximum age of the required job execution to consider it valid.
        /// If the required job hasn't run within this window, the dependency is not met.
        /// </summary>
        TimeSpan? MaxAge { get; }
    }

    /// <summary>
    /// Interface for tracking and resolving CronJob dependencies.
    /// </summary>
    public interface ICronJobDependencyTracker
    {
        /// <summary>
        /// Registers a dependency between jobs.
        /// </summary>
        /// <param name="dependency">The dependency to register.</param>
        void RegisterDependency(ICronJobDependency dependency);

        /// <summary>
        /// Gets all dependencies for a specific job.
        /// </summary>
        /// <param name="jobName">The name of the dependent job.</param>
        /// <returns>The dependencies for the job.</returns>
        IReadOnlyList<ICronJobDependency> GetDependencies(string jobName);

        /// <summary>
        /// Checks if all dependencies for a job are satisfied.
        /// </summary>
        /// <param name="jobName">The name of the dependent job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if all dependencies are satisfied, false otherwise.</returns>
        Task<bool> AreDependenciesSatisfiedAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the unsatisfied dependencies for a job.
        /// </summary>
        /// <param name="jobName">The name of the dependent job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of unsatisfied dependency names.</returns>
        Task<IReadOnlyList<string>> GetUnsatisfiedDependenciesAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that a job has completed execution.
        /// </summary>
        /// <param name="jobName">The name of the completed job.</param>
        /// <param name="success">Whether the job succeeded.</param>
        /// <param name="executionId">The execution ID.</param>
        Task RecordCompletionAsync(string jobName, bool success, Guid executionId);

        /// <summary>
        /// Gets jobs that are waiting for a specific job to complete.
        /// </summary>
        /// <param name="jobName">The name of the required job.</param>
        /// <returns>List of dependent job names.</returns>
        IReadOnlyList<string> GetDependentJobs(string jobName);
    }

    /// <summary>
    /// Represents a completion record for a job execution.
    /// </summary>
    public sealed class JobCompletionRecord
    {
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        public string JobName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the execution ID.
        /// </summary>
        public Guid ExecutionId { get; init; }

        /// <summary>
        /// Gets the completion time.
        /// </summary>
        public DateTimeOffset CompletedAt { get; init; }

        /// <summary>
        /// Gets whether the execution was successful.
        /// </summary>
        public bool Success { get; init; }
    }

    /// <summary>
    /// Default implementation of <see cref="ICronJobDependency"/>.
    /// </summary>
    public sealed class CronJobDependency : ICronJobDependency
    {
        /// <summary>
        /// Creates a new job dependency.
        /// </summary>
        /// <param name="dependentJobName">The name of the dependent job.</param>
        /// <param name="requiredJobNames">The names of required jobs.</param>
        /// <param name="requireSuccess">Whether required jobs must succeed.</param>
        /// <param name="maxAge">Maximum age of required job execution.</param>
        public CronJobDependency(
            string dependentJobName,
            IReadOnlyList<string> requiredJobNames,
            bool requireSuccess = true,
            TimeSpan? maxAge = null)
        {
            DependentJobName = dependentJobName ?? throw new ArgumentNullException(nameof(dependentJobName));
            RequiredJobNames = requiredJobNames ?? throw new ArgumentNullException(nameof(requiredJobNames));
            RequireSuccess = requireSuccess;
            MaxAge = maxAge;
        }

        /// <inheritdoc />
        public string DependentJobName { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> RequiredJobNames { get; }

        /// <inheritdoc />
        public bool RequireSuccess { get; }

        /// <inheritdoc />
        public TimeSpan? MaxAge { get; }

        /// <summary>
        /// Creates a builder for constructing dependencies.
        /// </summary>
        /// <param name="dependentJobName">The name of the dependent job.</param>
        /// <returns>A dependency builder.</returns>
        public static CronJobDependencyBuilder For(string dependentJobName)
        {
            return new CronJobDependencyBuilder(dependentJobName);
        }
    }

    /// <summary>
    /// Fluent builder for creating CronJob dependencies.
    /// </summary>
    public sealed class CronJobDependencyBuilder
    {
        private readonly string _dependentJobName;
        private readonly List<string> _requiredJobs = new();
        private bool _requireSuccess = true;
        private TimeSpan? _maxAge;

        internal CronJobDependencyBuilder(string dependentJobName)
        {
            _dependentJobName = dependentJobName;
        }

        /// <summary>
        /// Adds a required job dependency.
        /// </summary>
        /// <param name="jobName">The name of the required job.</param>
        /// <returns>This builder for chaining.</returns>
        public CronJobDependencyBuilder DependsOn(string jobName)
        {
            _requiredJobs.Add(jobName);
            return this;
        }

        /// <summary>
        /// Adds a required job dependency using the job type name.
        /// </summary>
        /// <typeparam name="T">The type of the required job.</typeparam>
        /// <returns>This builder for chaining.</returns>
        public CronJobDependencyBuilder DependsOn<T>() where T : class
        {
            _requiredJobs.Add(typeof(T).Name);
            return this;
        }

        /// <summary>
        /// Sets whether required jobs must succeed (not just complete).
        /// </summary>
        /// <param name="requireSuccess">True to require success, false to allow failure.</param>
        /// <returns>This builder for chaining.</returns>
        public CronJobDependencyBuilder WithSuccessRequired(bool requireSuccess = true)
        {
            _requireSuccess = requireSuccess;
            return this;
        }

        /// <summary>
        /// Sets the maximum age for considering a dependency satisfied.
        /// </summary>
        /// <param name="maxAge">The maximum age.</param>
        /// <returns>This builder for chaining.</returns>
        public CronJobDependencyBuilder WithMaxAge(TimeSpan maxAge)
        {
            _maxAge = maxAge;
            return this;
        }

        /// <summary>
        /// Builds the dependency.
        /// </summary>
        /// <returns>The created dependency.</returns>
        public ICronJobDependency Build()
        {
            return new CronJobDependency(_dependentJobName, _requiredJobs, _requireSuccess, _maxAge);
        }
    }
}

