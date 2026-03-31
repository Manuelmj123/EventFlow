using System.Text.Json;
using EventFlow.Application.Abstractions;
using EventFlow.Contracts.Events;
using EventFlow.Domain.Entities;

namespace EventFlow.Application.UseCases.StartWorkflow;

public sealed class StartWorkflowCommandHandler
{
    private const string RoutingKey = "workflow.started";

    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowEventLogRepository _eventLogRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public StartWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowEventLogRepository eventLogRepository,
        IMessagePublisher messagePublisher,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _eventLogRepository = eventLogRepository;
        _messagePublisher = messagePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        StartWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var workflow = new Workflow(
            command.Name,
            command.PayloadJson,
            correlationId);

        await _workflowRepository.AddAsync(workflow, cancellationToken);

        var integrationEvent = new WorkflowStartedEvent
        {
            WorkflowId = workflow.Id,
            CorrelationId = correlationId,
            EventName = RoutingKey,
            Name = workflow.Name,
            PayloadJson = workflow.PayloadJson
        };

        var eventLog = new WorkflowEventLog(
            workflow.Id,
            RoutingKey,
            "Published",
            JsonSerializer.Serialize(integrationEvent),
            "API");

        await _eventLogRepository.AddAsync(eventLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _messagePublisher.PublishAsync(
            integrationEvent,
            RoutingKey,
            cancellationToken);

        return workflow.Id;
    }
}