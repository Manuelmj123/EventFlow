using EventFlow.Application.Abstractions;
using EventFlow.Domain.Entities;
using EventFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories;

public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly EventFlowDbContext _dbContext;

    public WorkflowRepository(EventFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Workflow?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workflows
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Workflows.AddAsync(workflow, cancellationToken);
    }

    public Task UpdateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Workflows.Update(workflow);
        return Task.CompletedTask;
    }
}