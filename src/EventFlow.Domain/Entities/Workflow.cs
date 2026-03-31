using EventFlow.Domain.Common;
using EventFlow.Domain.Enums;

namespace EventFlow.Domain.Entities;

public class Workflow : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Pending;
    public string PayloadJson { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;

    private Workflow()
    {
    }

    public Workflow(
        string name,
        string payloadJson,
        string correlationId)
    {
        SetName(name);
        SetPayloadJson(payloadJson);
        SetCorrelationId(correlationId);
        Status = WorkflowStatus.Pending;
    }

    public void MarkValidated()
    {
        Status = WorkflowStatus.Validated;
        SetUpdatedUtc();
    }

    public void MarkProcessed()
    {
        Status = WorkflowStatus.Processed;
        SetUpdatedUtc();
    }

    public void MarkCompleted()
    {
        Status = WorkflowStatus.Completed;
        SetUpdatedUtc();
    }

    public void MarkFailed()
    {
        Status = WorkflowStatus.Failed;
        SetUpdatedUtc();
    }

    public void UpdatePayload(string payloadJson)
    {
        SetPayloadJson(payloadJson);
        SetUpdatedUtc();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workflow name is required.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Workflow name cannot exceed 200 characters.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetPayloadJson(string payloadJson)
    {
        PayloadJson = payloadJson?.Trim() ?? string.Empty;
    }

    private void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        }

        if (correlationId.Length > 100)
        {
            throw new ArgumentException("CorrelationId cannot exceed 100 characters.", nameof(correlationId));
        }

        CorrelationId = correlationId.Trim();
    }
}