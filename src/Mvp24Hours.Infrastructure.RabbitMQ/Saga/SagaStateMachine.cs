//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Base class for implementing saga state machines with event-driven transitions.
    /// Provides a declarative way to define states, events, and transitions.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <remarks>
    /// <para>
    /// A saga state machine manages the lifecycle of a saga through:
    /// <list type="bullet">
    /// <item>States: Named stages in the saga lifecycle (Initial, Processing, Completed, etc.)</item>
    /// <item>Events: Messages that trigger state transitions</item>
    /// <item>Transitions: Rules defining how events change states</item>
    /// <item>Actions: Side effects executed during transitions</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderSagaStateMachine : SagaStateMachine&lt;OrderSagaData&gt;
    /// {
    ///     public OrderSagaStateMachine()
    ///     {
    ///         // Define states
    ///         State("AwaitingPayment");
    ///         State("AwaitingShipment");
    ///         State("Shipped");
    ///         
    ///         // Define events and transitions
    ///         Initially(
    ///             When&lt;OrderCreatedEvent&gt;()
    ///                 .TransitionTo("AwaitingPayment")
    ///                 .ThenAsync(async ctx => await ctx.PublishAsync(new ProcessPaymentCommand()))
    ///         );
    ///         
    ///         During("AwaitingPayment",
    ///             When&lt;PaymentCompletedEvent&gt;()
    ///                 .TransitionTo("AwaitingShipment")
    ///                 .ThenAsync(async ctx => await ctx.PublishAsync(new ShipOrderCommand())),
    ///             When&lt;PaymentFailedEvent&gt;()
    ///                 .Finalize()
    ///         );
    ///         
    ///         During("AwaitingShipment",
    ///             When&lt;OrderShippedEvent&gt;()
    ///                 .TransitionTo("Shipped")
    ///                 .Finalize()
    ///         );
    ///         
    ///         // Set final states
    ///         SetCompletedWhenEnter("Shipped");
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class SagaStateMachine<TData> where TData : class, new()
    {
        private readonly Dictionary<string, SagaState> _states = new();
        private readonly Dictionary<Type, List<EventHandler>> _initialHandlers = new();
        private readonly Dictionary<string, Dictionary<Type, List<EventHandler>>> _stateHandlers = new();
        private readonly HashSet<string> _finalStates = new();
        private readonly List<Action<SagaInstance<TData>>> _onCompletedCallbacks = new();
        private readonly List<Action<SagaInstance<TData>, Exception>> _onFaultedCallbacks = new();
        
        protected ILogger? Logger { get; set; }

        /// <summary>
        /// Gets the initial state name.
        /// </summary>
        public string InitialState { get; protected set; } = "Initial";

        /// <summary>
        /// Gets the saga type name.
        /// </summary>
        public virtual string SagaTypeName => GetType().Name;

        /// <summary>
        /// Defines a state in the state machine.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <returns>The state for fluent configuration.</returns>
        protected SagaState State(string name)
        {
            if (!_states.ContainsKey(name))
            {
                var state = new SagaState(name);
                _states[name] = state;
                _stateHandlers[name] = new Dictionary<Type, List<EventHandler>>();
            }
            return _states[name];
        }

        /// <summary>
        /// Configures handlers for the initial state.
        /// </summary>
        /// <param name="handlers">The event handlers.</param>
        protected void Initially(params EventHandler[] handlers)
        {
            foreach (var handler in handlers)
            {
                if (!_initialHandlers.ContainsKey(handler.EventType))
                {
                    _initialHandlers[handler.EventType] = new List<EventHandler>();
                }
                _initialHandlers[handler.EventType].Add(handler);
            }
        }

        /// <summary>
        /// Configures handlers for a specific state.
        /// </summary>
        /// <param name="stateName">The state name.</param>
        /// <param name="handlers">The event handlers.</param>
        protected void During(string stateName, params EventHandler[] handlers)
        {
            State(stateName); // Ensure state exists
            foreach (var handler in handlers)
            {
                if (!_stateHandlers[stateName].ContainsKey(handler.EventType))
                {
                    _stateHandlers[stateName][handler.EventType] = new List<EventHandler>();
                }
                _stateHandlers[stateName][handler.EventType].Add(handler);
            }
        }

        /// <summary>
        /// Creates an event handler builder for a specific event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>The event handler builder.</returns>
        protected EventHandlerBuilder<TEvent> When<TEvent>() where TEvent : class
        {
            return new EventHandlerBuilder<TEvent>(this);
        }

        /// <summary>
        /// Marks a state as a final/completion state.
        /// </summary>
        /// <param name="stateName">The state name.</param>
        protected void SetCompletedWhenEnter(string stateName)
        {
            _finalStates.Add(stateName);
        }

        /// <summary>
        /// Registers a callback for when the saga completes.
        /// </summary>
        /// <param name="callback">The callback action.</param>
        protected void OnCompleted(Action<SagaInstance<TData>> callback)
        {
            _onCompletedCallbacks.Add(callback);
        }

        /// <summary>
        /// Registers a callback for when the saga faults.
        /// </summary>
        /// <param name="callback">The callback action.</param>
        protected void OnFaulted(Action<SagaInstance<TData>, Exception> callback)
        {
            _onFaultedCallbacks.Add(callback);
        }

        /// <summary>
        /// Processes an event against a saga instance.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="instance">The saga instance.</param>
        /// <param name="event">The event.</param>
        /// <param name="context">The consume context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        public async Task<bool> ProcessEventAsync<TEvent>(
            SagaInstance<TData> instance,
            TEvent @event,
            IConsumeContext<TEvent> context,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            var eventType = typeof(TEvent);
            List<EventHandler>? handlers = null;

            // Find handlers based on current state
            if (instance.CurrentState == InitialState)
            {
                _initialHandlers.TryGetValue(eventType, out handlers);
            }
            else if (_stateHandlers.TryGetValue(instance.CurrentState, out var stateDict))
            {
                stateDict.TryGetValue(eventType, out handlers);
            }

            if (handlers == null || handlers.Count == 0)
            {
                Logger?.LogWarning(
                    "No handler found for event {EventType} in state {State} for saga {SagaId}",
                    eventType.Name, instance.CurrentState, instance.CorrelationId);
                return false;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(instance, @event, context, cancellationToken);

                    // Check if we should mark as completed
                    if (_finalStates.Contains(instance.CurrentState) && !instance.IsCompleted)
                    {
                        instance.Complete();
                        foreach (var callback in _onCompletedCallbacks)
                        {
                            callback(instance);
                        }
                    }

                    Logger?.LogDebug(
                        "Processed event {EventType} for saga {SagaId}, new state: {State}",
                        eventType.Name, instance.CorrelationId, instance.CurrentState);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex,
                        "Error processing event {EventType} for saga {SagaId}",
                        eventType.Name, instance.CorrelationId);

                    instance.Fault(ex.Message);
                    foreach (var callback in _onFaultedCallbacks)
                    {
                        callback(instance, ex);
                    }
                    throw;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if an event can start a new saga instance.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>True if the event can start a saga.</returns>
        public bool CanEventStartSaga<TEvent>() where TEvent : class
        {
            return _initialHandlers.ContainsKey(typeof(TEvent));
        }

        /// <summary>
        /// Gets the correlation ID extraction function for an event type.
        /// Override in derived classes to provide custom correlation logic.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="event">The event.</param>
        /// <returns>The correlation ID.</returns>
        public virtual Guid GetCorrelationId<TEvent>(TEvent @event) where TEvent : class
        {
            // Try common property names
            var type = typeof(TEvent);
            
            var correlationIdProp = type.GetProperty("CorrelationId");
            if (correlationIdProp?.PropertyType == typeof(Guid))
            {
                return (Guid)correlationIdProp.GetValue(@event)!;
            }

            var sagaIdProp = type.GetProperty("SagaId");
            if (sagaIdProp?.PropertyType == typeof(Guid))
            {
                return (Guid)sagaIdProp.GetValue(@event)!;
            }

            var idProp = type.GetProperty("Id");
            if (idProp?.PropertyType == typeof(Guid))
            {
                return (Guid)idProp.GetValue(@event)!;
            }

            throw new InvalidOperationException(
                $"Cannot extract correlation ID from {type.Name}. " +
                $"Override GetCorrelationId or add CorrelationId/SagaId/Id property.");
        }

        #region Nested Types

        /// <summary>
        /// Represents a state in the state machine.
        /// </summary>
        public class SagaState
        {
            public string Name { get; }
            public Action<SagaInstance<TData>>? OnEnter { get; set; }
            public Action<SagaInstance<TData>>? OnLeave { get; set; }

            public SagaState(string name)
            {
                Name = name;
            }

            public SagaState OnEnterDo(Action<SagaInstance<TData>> action)
            {
                OnEnter = action;
                return this;
            }

            public SagaState OnLeaveDo(Action<SagaInstance<TData>> action)
            {
                OnLeave = action;
                return this;
            }
        }

        /// <summary>
        /// Represents an event handler in the state machine.
        /// </summary>
        public class EventHandler
        {
            public Type EventType { get; set; } = typeof(object);
            public string? TargetState { get; set; }
            public bool IsFinalizing { get; set; }
            public Func<SagaInstance<TData>, object, object, CancellationToken, Task>? Action { get; set; }
            public Func<SagaInstance<TData>, object, bool>? Condition { get; set; }

            public async Task HandleAsync<TEvent>(
                SagaInstance<TData> instance,
                TEvent @event,
                object context,
                CancellationToken cancellationToken) where TEvent : class
            {
                // Check condition
                if (Condition != null && !Condition(instance, @event!))
                {
                    return;
                }

                // Execute action
                if (Action != null)
                {
                    await Action(instance, @event!, context, cancellationToken);
                }

                // Transition state
                if (!string.IsNullOrEmpty(TargetState))
                {
                    instance.TransitionTo(TargetState, $"Event: {typeof(TEvent).Name}");
                }

                // Finalize if needed
                if (IsFinalizing && !instance.IsCompleted)
                {
                    instance.Complete();
                }
            }
        }

        /// <summary>
        /// Builder for creating event handlers.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        public class EventHandlerBuilder<TEvent> where TEvent : class
        {
            private readonly SagaStateMachine<TData> _machine;
            private readonly EventHandler _handler;

            public EventHandlerBuilder(SagaStateMachine<TData> machine)
            {
                _machine = machine;
                _handler = new EventHandler { EventType = typeof(TEvent) };
            }

            /// <summary>
            /// Sets the target state for the transition.
            /// </summary>
            /// <param name="stateName">The target state name.</param>
            /// <returns>The builder for chaining.</returns>
            public EventHandlerBuilder<TEvent> TransitionTo(string stateName)
            {
                _machine.State(stateName); // Ensure state exists
                _handler.TargetState = stateName;
                return this;
            }

            /// <summary>
            /// Adds a condition that must be true for the handler to execute.
            /// </summary>
            /// <param name="condition">The condition predicate.</param>
            /// <returns>The builder for chaining.</returns>
            public EventHandlerBuilder<TEvent> If(Func<SagaInstance<TData>, TEvent, bool> condition)
            {
                _handler.Condition = (instance, evt) => condition(instance, (TEvent)evt);
                return this;
            }

            /// <summary>
            /// Adds an async action to execute during the transition.
            /// </summary>
            /// <param name="action">The async action.</param>
            /// <returns>The builder for chaining.</returns>
            public EventHandlerBuilder<TEvent> ThenAsync(Func<SagaEventContext<TData, TEvent>, Task> action)
            {
                _handler.Action = async (instance, evt, ctx, ct) =>
                {
                    var eventContext = new SagaEventContext<TData, TEvent>(instance, (TEvent)evt, ctx, ct);
                    await action(eventContext);
                };
                return this;
            }

            /// <summary>
            /// Adds a sync action to execute during the transition.
            /// </summary>
            /// <param name="action">The sync action.</param>
            /// <returns>The builder for chaining.</returns>
            public EventHandlerBuilder<TEvent> Then(Action<SagaEventContext<TData, TEvent>> action)
            {
                _handler.Action = (instance, evt, ctx, ct) =>
                {
                    var eventContext = new SagaEventContext<TData, TEvent>(instance, (TEvent)evt, ctx, ct);
                    action(eventContext);
                    return Task.CompletedTask;
                };
                return this;
            }

            /// <summary>
            /// Marks this handler as finalizing the saga.
            /// </summary>
            /// <returns>The builder for chaining.</returns>
            public EventHandlerBuilder<TEvent> Finalize()
            {
                _handler.IsFinalizing = true;
                return this;
            }

            /// <summary>
            /// Implicit conversion to EventHandler.
            /// </summary>
            public static implicit operator EventHandler(EventHandlerBuilder<TEvent> builder)
            {
                return builder._handler;
            }
        }

        #endregion
    }

    /// <summary>
    /// Context passed to saga event handlers.
    /// </summary>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public class SagaEventContext<TData, TEvent>
        where TData : class, new()
        where TEvent : class
    {
        private readonly object _consumeContext;

        public SagaEventContext(
            SagaInstance<TData> instance,
            TEvent @event,
            object consumeContext,
            CancellationToken cancellationToken)
        {
            Instance = instance;
            Event = @event;
            _consumeContext = consumeContext;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets the saga instance.
        /// </summary>
        public SagaInstance<TData> Instance { get; }

        /// <summary>
        /// Gets the saga data.
        /// </summary>
        public TData Data => Instance.Data;

        /// <summary>
        /// Gets the event.
        /// </summary>
        public TEvent Event { get; }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the saga ID.
        /// </summary>
        public Guid SagaId => Instance.CorrelationId;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public string CurrentState => Instance.CurrentState;

        /// <summary>
        /// Sets metadata on the saga instance.
        /// </summary>
        public void SetMetadata(string key, string value)
        {
            Instance.Metadata[key] = value;
        }

        /// <summary>
        /// Gets metadata from the saga instance.
        /// </summary>
        public string? GetMetadata(string key)
        {
            return Instance.Metadata.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Publishes a message using the consume context.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">Optional routing key.</param>
        public async Task PublishAsync<T>(T message, string? routingKey = null) where T : class
        {
            if (_consumeContext is IConsumeContext<TEvent> ctx)
            {
                await ctx.PublishAsync(message, routingKey, CancellationToken);
            }
        }
    }
}

