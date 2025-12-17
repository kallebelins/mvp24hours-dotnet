//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.DependencyGraph;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow
{
    /// <summary>
    /// Extension methods for registering advanced pipeline flow services.
    /// </summary>
    public static class AdvancedFlowServiceExtensions
    {
        /// <summary>
        /// Adds the in-memory checkpoint store for checkpointable pipelines.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemoryCheckpointStore(this IServiceCollection services)
        {
            services.AddSingleton<ICheckpointStore, InMemoryCheckpointStore>();
            return services;
        }

        /// <summary>
        /// Adds a custom checkpoint store implementation.
        /// </summary>
        /// <typeparam name="TStore">The checkpoint store implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCheckpointStore<TStore>(this IServiceCollection services)
            where TStore : class, ICheckpointStore
        {
            services.AddSingleton<ICheckpointStore, TStore>();
            return services;
        }

        /// <summary>
        /// Adds a checkpointable pipeline factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configure checkpoint options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCheckpointablePipeline(
            this IServiceCollection services,
            Action<CheckpointOptions>? configureOptions = null)
        {
            var options = new CheckpointOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<IStateSerializer, JsonStateSerializer>();

            return services;
        }

        /// <summary>
        /// Adds a dependency graph executor.
        /// </summary>
        /// <typeparam name="TContext">The context type for the dependency graph.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configure graph options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDependencyGraphExecutor<TContext>(
            this IServiceCollection services,
            Action<DependencyGraphOptions>? configureOptions = null)
        {
            var options = new DependencyGraphOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddTransient<DependencyGraph<TContext>>();

            return services;
        }

        /// <summary>
        /// Adds a pipeline saga orchestrator.
        /// </summary>
        /// <typeparam name="TContext">The saga context type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configure saga options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineSaga<TContext>(
            this IServiceCollection services,
            Action<PipelineSagaOptions>? configureOptions = null)
        {
            var options = new PipelineSagaOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddTransient<PipelineSagaOrchestrator<TContext>>();

            return services;
        }

        /// <summary>
        /// Adds an in-memory saga state store.
        /// </summary>
        /// <typeparam name="TContext">The saga context type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemorySagaStateStore<TContext>(this IServiceCollection services)
        {
            services.AddSingleton<IPipelineSagaStateStore<TContext>, InMemorySagaStateStore<TContext>>();
            return services;
        }

        /// <summary>
        /// Adds all advanced flow services with default configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAdvancedPipelineFlow(this IServiceCollection services)
        {
            services.AddInMemoryCheckpointStore();
            services.AddCheckpointablePipeline();
            return services;
        }
    }

    /// <summary>
    /// In-memory saga state store implementation.
    /// </summary>
    /// <typeparam name="TContext">The saga context type.</typeparam>
    public sealed class InMemorySagaStateStore<TContext> : IPipelineSagaStateStore<TContext>
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SagaPersistedState<TContext>> _states = new();

        /// <inheritdoc/>
        public System.Threading.Tasks.Task SaveStateAsync(string sagaId, SagaPersistedState<TContext> state, System.Threading.CancellationToken cancellationToken = default)
        {
            _states[sagaId] = state;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<SagaPersistedState<TContext>?> LoadStateAsync(string sagaId, System.Threading.CancellationToken cancellationToken = default)
        {
            _states.TryGetValue(sagaId, out var state);
            return System.Threading.Tasks.Task.FromResult(state);
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task DeleteStateAsync(string sagaId, System.Threading.CancellationToken cancellationToken = default)
        {
            _states.TryRemove(sagaId, out _);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Gets the number of stored states.
        /// </summary>
        public int Count => _states.Count;

        /// <summary>
        /// Clears all stored states.
        /// </summary>
        public void Clear() => _states.Clear();
    }
}

