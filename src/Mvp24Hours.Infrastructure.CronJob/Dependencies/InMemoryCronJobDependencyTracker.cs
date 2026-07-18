//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Dependencies;

/// <summary>
/// In-memory implementation of <see cref="ICronJobDependencyTracker"/>.
/// Tracks job dependencies and completion records.
/// </summary>
public sealed class InMemoryCronJobDependencyTracker : ICronJobDependencyTracker
{
    private readonly ConcurrentDictionary<string, List<ICronJobDependency>> _dependencies = new();
    private readonly ConcurrentDictionary<string, JobCompletionRecord> _completions = new();
    private readonly ConcurrentDictionary<string, List<string>> _reverseDependencies = new();

    /// <inheritdoc />
    public void RegisterDependency(ICronJobDependency dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency, nameof(dependency));

        _dependencies.AddOrUpdate(
            dependency.DependentJobName,
            _ => [dependency],
            (_, list) =>
            {
                list.Add(dependency);
                return list;
            });

        // Update reverse dependencies for efficient lookup
        foreach (string requiredJob in dependency.RequiredJobNames)
        {
            _reverseDependencies.AddOrUpdate(
                requiredJob,
                _ => [dependency.DependentJobName],
                (_, list) =>
                {
                    if (!list.Contains(dependency.DependentJobName))
                    {
                        list.Add(dependency.DependentJobName);
                    }
                    return list;
                });
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ICronJobDependency> GetDependencies(string jobName)
    {
        if (_dependencies.TryGetValue(jobName, out List<ICronJobDependency>? deps))
        {
            return [.. deps];
        }
        return [];
    }

    /// <inheritdoc />
    public Task<bool> AreDependenciesSatisfiedAsync(string jobName, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ICronJobDependency> dependencies = GetDependencies(jobName);

        if (dependencies.Count == 0)
        {
            return Task.FromResult(true);
        }

        foreach (ICronJobDependency dependency in dependencies)
        {
            foreach (string requiredJob in dependency.RequiredJobNames)
            {
                if (!IsDependencySatisfied(requiredJob, dependency))
                {
                    return Task.FromResult(false);
                }
            }
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetUnsatisfiedDependenciesAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var unsatisfied = new List<string>();
        IReadOnlyList<ICronJobDependency> dependencies = GetDependencies(jobName);

        foreach (ICronJobDependency dependency in dependencies)
        {
            foreach (string requiredJob in dependency.RequiredJobNames)
            {
                if (!IsDependencySatisfied(requiredJob, dependency))
                {
                    unsatisfied.Add(requiredJob);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(unsatisfied);
    }

    /// <inheritdoc />
    public Task RecordCompletionAsync(string jobName, bool success, Guid executionId)
    {
        var record = new JobCompletionRecord
        {
            JobName = jobName,
            ExecutionId = executionId,
            CompletedAt = DateTimeOffset.UtcNow,
            Success = success
        };

        _completions[jobName] = record;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDependentJobs(string jobName)
    {
        if (_reverseDependencies.TryGetValue(jobName, out List<string>? dependents))
        {
            return [.. dependents];
        }
        return [];
    }

    private bool IsDependencySatisfied(string requiredJobName, ICronJobDependency dependency)
    {
        if (!_completions.TryGetValue(requiredJobName, out JobCompletionRecord? completion))
        {
            return false;
        }

        // Check if success is required
        if (dependency.RequireSuccess && !completion.Success)
        {
            return false;
        }

        // Check max age
        if (dependency.MaxAge.HasValue)
        {
            TimeSpan age = DateTimeOffset.UtcNow - completion.CompletedAt;
            if (age > dependency.MaxAge.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Clears all completion records.
    /// </summary>
    public void ClearCompletions()
    {
        _completions.Clear();
    }

    /// <summary>
    /// Clears all dependencies and completion records.
    /// </summary>
    public void Clear()
    {
        _dependencies.Clear();
        _completions.Clear();
        _reverseDependencies.Clear();
    }
}

