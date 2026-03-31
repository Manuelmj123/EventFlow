using EventFlow.Contracts.Common;

namespace EventFlow.Contracts.Events;

public sealed class WorkflowProcessedEvent : IntegrationEvent
{
    public string Name { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public string ResultJson { get; init; } = string.Empty;
}