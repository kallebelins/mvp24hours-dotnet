//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;
using Mvp24Hours.Infrastructure.Cqrs.Observability;
using Mvp24Hours.Infrastructure.Cqrs.Saga;
using Mvp24Hours.Infrastructure.Cqrs.Scheduling;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Cqrs.Serialization;

/// <summary>
/// Source-generated JSON serializer context for CQRS module types.
/// Provides AOT-friendly serialization for domain events, integration events, and saga data.
/// </summary>
/// <remarks>
/// Event IDs are prefixed with "Cqrs" for clarity in distributed tracing.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
// Domain Events
[JsonSerializable(typeof(DomainEventBase))]
[JsonSerializable(typeof(List<DomainEventBase>))]
// Integration Events
[JsonSerializable(typeof(IntegrationEventBase))]
[JsonSerializable(typeof(IIntegrationEvent))]
// Event Sourcing
[JsonSerializable(typeof(StoredEvent))]
[JsonSerializable(typeof(EventStream))]
[JsonSerializable(typeof(List<StoredEvent>))]
[JsonSerializable(typeof(Snapshot))]
// Inbox/Outbox
[JsonSerializable(typeof(InboxMessage))]
[JsonSerializable(typeof(OutboxMessage))]
[JsonSerializable(typeof(List<InboxMessage>))]
[JsonSerializable(typeof(List<OutboxMessage>))]
// Saga
[JsonSerializable(typeof(SagaState))]
[JsonSerializable(typeof(SagaResult))]
[JsonSerializable(typeof(CompensationResult))]
[JsonSerializable(typeof(List<SagaResult>))]
// Scheduling
[JsonSerializable(typeof(ScheduledCommandEntry))]
[JsonSerializable(typeof(List<ScheduledCommandEntry>))]
// Observability
[JsonSerializable(typeof(RequestContext))]
[JsonSerializable(typeof(AuditEntry))]
[JsonSerializable(typeof(List<AuditEntry>))]
// Common types
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
public partial class CqrsJsonSerializerContext : JsonSerializerContext
{
    /// <summary>
    /// Gets the default serializer options configured for CQRS module.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => Default.Options;

    /// <summary>
    /// Creates a new JsonSerializerOptions instance with the source-generated type info.
    /// </summary>
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        options.TypeInfoResolverChain.Add(Default);
        return options;
    }
}

