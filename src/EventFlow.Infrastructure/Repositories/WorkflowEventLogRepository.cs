using EventFlow.Application.Abstractions;
using EventFlow.Domain.Entities;
using EventFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories;

public sealed class WorkflowEventLogRepository : IWorkflowEventLogRepository
{
    private readonly EventFlowDbContext _dbContext;

    public WorkflowEventLogRepository(EventFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        WorkflowEventLog eventLog,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowEventLogs.AddAsync(eventLog, cancellationToken);
    }

    public async Task<List<WorkflowEventLog>> GetByWorkflowIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowEventLogs
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}