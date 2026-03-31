using EventFlow.Contracts.Common;

namespace EventFlow.Contracts.Events;

public sealed class WorkflowFailedEvent : IntegrationEvent
{
    public string Name { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string FailedBy { get; init; } = string.Empty;
}