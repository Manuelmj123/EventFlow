using EventFlow.Domain.Common;

namespace EventFlow.Domain.Entities;

public class WorkflowEventLog : BaseEntity
{
    public Guid WorkflowId { get; private set; }
    public string EventName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string ConsumerName { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }

    private WorkflowEventLog()
    {
    }

    public WorkflowEventLog(
        Guid workflowId,
        string eventName,
        string status,
        string payloadJson,
        string consumerName,
        int retryCount = 0)
    {
        SetWorkflowId(workflowId);
        SetEventName(eventName);
        SetStatus(status);
        SetPayloadJson(payloadJson);
        SetConsumerName(consumerName);
        SetRetryCount(retryCount);
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
        SetUpdatedUtc();
    }

    private void SetWorkflowId(Guid workflowId)
    {
        if (workflowId == Guid.Empty)
        {
            throw new ArgumentException("WorkflowId is required.", nameof(workflowId));
        }

        WorkflowId = workflowId;
    }

    private void SetEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name is required.", nameof(eventName));
        }

        if (eventName.Length > 200)
        {
            throw new ArgumentException("Event name cannot exceed 200 characters.", nameof(eventName));
        }

        EventName = eventName.Trim();
    }

    private void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Event status is required.", nameof(status));
        }

        if (status.Length > 100)
        {
            throw new ArgumentException("Event status cannot exceed 100 characters.", nameof(status));
        }

        Status = status.Trim();
    }

    private void SetPayloadJson(string payloadJson)
    {
        PayloadJson = payloadJson?.Trim() ?? string.Empty;
    }

    private void SetConsumerName(string consumerName)
    {
        ConsumerName = consumerName?.Trim() ?? string.Empty;
    }

    private void SetRetryCount(int retryCount)
    {
        if (retryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count cannot be negative.");
        }

        RetryCount = retryCount;
    }
}