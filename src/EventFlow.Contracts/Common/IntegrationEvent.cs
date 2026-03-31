namespace EventFlow.Contracts.Common;

public abstract class IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid WorkflowId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public string EventName { get; init; } = string.Empty;
}