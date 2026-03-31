using EventFlow.Domain.Entities;

namespace EventFlow.Application.Abstractions;

public interface IWorkflowEventLogRepository
{
    Task AddAsync(
        WorkflowEventLog eventLog,
        CancellationToken cancellationToken = default);

    Task<List<WorkflowEventLog>> GetByWorkflowIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);
}