using Mvp24Hours.Core.Contract.Domain.Entity;

namespace App.Core.Events;

/// <summary>
/// Domain event raised when a new item is created.
/// </summary>
public sealed record ItemCreatedDomainEvent(int ItemId, string ItemName) : DomainEventBase;
