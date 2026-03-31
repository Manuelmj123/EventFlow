using EventFlow.Application.Abstractions;
using EventFlow.Application.UseCases.StartWorkflow;
using EventFlow.Contracts.Requests;
using EventFlow.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly StartWorkflowCommandHandler _startWorkflowCommandHandler;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowEventLogRepository _workflowEventLogRepository;

    public WorkflowsController(
        StartWorkflowCommandHandler startWorkflowCommandHandler,
        IWorkflowRepository workflowRepository,
        IWorkflowEventLogRepository workflowEventLogRepository)
    {
        _startWorkflowCommandHandler = startWorkflowCommandHandler;
        _workflowRepository = workflowRepository;
        _workflowEventLogRepository = workflowEventLogRepository;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowResponse>> CreateAsync(
        [FromBody] StartWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = new StartWorkflowCommand(
            request.Name,
            request.PayloadJson);

        var workflowId = await _startWorkflowCommandHandler.HandleAsync(command, cancellationToken);

        var workflow = await _workflowRepository.GetByIdAsync(workflowId, cancellationToken);

        if (workflow is null)
        {
            return Problem("Workflow was created but could not be retrieved.");
        }

        var response = new WorkflowResponse
        {
            WorkflowId = workflow.Id,
            Name = workflow.Name,
            Status = workflow.Status.ToString(),
            CorrelationId = workflow.CorrelationId,
            CreatedAtUtc = workflow.CreatedAtUtc,
            UpdatedAtUtc = workflow.UpdatedAtUtc
        };

        return CreatedAtRoute(
            "GetWorkflowById",
            new { id = workflow.Id },
            response);
    }

    [HttpGet("{id:guid}", Name = "GetWorkflowById")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);

        if (workflow is null)
        {
            return NotFound();
        }

        var response = new WorkflowResponse
        {
            WorkflowId = workflow.Id,
            Name = workflow.Name,
            Status = workflow.Status.ToString(),
            CorrelationId = workflow.CorrelationId,
            CreatedAtUtc = workflow.CreatedAtUtc,
            UpdatedAtUtc = workflow.UpdatedAtUtc
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEventsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id, cancellationToken);

        if (workflow is null)
        {
            return NotFound();
        }

        var events = await _workflowEventLogRepository.GetByWorkflowIdAsync(id, cancellationToken);

        var response = events.Select(x => new
        {
            x.Id,
            x.WorkflowId,
            x.EventName,
            x.Status,
            x.PayloadJson,
            x.ConsumerName,
            x.RetryCount,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        });

        return Ok(response);
    }
}