using EventFlow.Application.Abstractions;
using EventFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Persistence;

public sealed class EventFlowDbContext : DbContext, IUnitOfWork
{
    public EventFlowDbContext(DbContextOptions<EventFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowEventLog> WorkflowEventLogs => Set<WorkflowEventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}