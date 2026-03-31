using EventFlow.Domain.Entities;

namespace EventFlow.Application.Abstractions;

public interface IWorkflowRepository
{
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Workflow workflow, CancellationToken cancellationToken = default);

    Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default);
}