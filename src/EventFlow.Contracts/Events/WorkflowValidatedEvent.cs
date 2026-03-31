using EventFlow.Contracts.Common;

namespace EventFlow.Contracts.Events;

public sealed class WorkflowValidatedEvent : IntegrationEvent
{
    public string Name { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
}