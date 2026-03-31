using EventFlow.Application.Abstractions;
using EventFlow.Contracts.Events;
using EventFlow.Domain.Entities;
using EventFlow.Infrastructure.Messaging;
using EventFlow.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventFlow.Worker.Validator;

public sealed class ValidatorWorker : BackgroundService
{
    private const string QueueName = "eventflow.workflow.started.q";
    private const string SuccessRoutingKey = "workflow.validated";
    private const string FailureRoutingKey = "workflow.failed";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ILogger<ValidatorWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public ValidatorWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILogger<ValidatorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = _connectionFactory.Create();
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel was not initialized.");
        }

        var channel = _channel;
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (_, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<WorkflowStartedEvent>(body);

                if (message is null)
                {
                    _logger.LogWarning("Received null or invalid workflow.started event.");
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await HandleMessageAsync(message, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validator worker failed processing message.");
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(
        WorkflowStartedEvent message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var workflowRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var eventLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowEventLogRepository>();
        var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var workflow = await workflowRepository.GetByIdAsync(message.WorkflowId, cancellationToken);

        if (workflow is null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in validator.", message.WorkflowId);
            return;
        }

        var isValid = !string.IsNullOrWhiteSpace(message.Name);

        if (isValid)
        {
            workflow.MarkValidated();
            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var validatedEvent = new WorkflowValidatedEvent
            {
                WorkflowId = workflow.Id,
                CorrelationId = workflow.CorrelationId,
                EventName = SuccessRoutingKey,
                Name = workflow.Name,
                PayloadJson = workflow.PayloadJson
            };

            var log = new WorkflowEventLog(
                workflow.Id,
                SuccessRoutingKey,
                "Published",
                JsonSerializer.Serialize(validatedEvent),
                nameof(ValidatorWorker));

            await eventLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await messagePublisher.PublishAsync(validatedEvent, SuccessRoutingKey, cancellationToken);

            _logger.LogInformation("Workflow {WorkflowId} validated.", workflow.Id);
            return;
        }

        workflow.MarkFailed();
        await workflowRepository.UpdateAsync(workflow, cancellationToken);

        var failedEvent = new WorkflowFailedEvent
        {
            WorkflowId = workflow.Id,
            CorrelationId = workflow.CorrelationId,
            EventName = FailureRoutingKey,
            Name = workflow.Name,
            PayloadJson = workflow.PayloadJson,
            ErrorMessage = "Validation failed.",
            FailedBy = nameof(ValidatorWorker)
        };

        var failedLog = new WorkflowEventLog(
            workflow.Id,
            FailureRoutingKey,
            "Published",
            JsonSerializer.Serialize(failedEvent),
            nameof(ValidatorWorker));

        await eventLogRepository.AddAsync(failedLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await messagePublisher.PublishAsync(failedEvent, FailureRoutingKey, cancellationToken);

        _logger.LogInformation("Workflow {WorkflowId} failed validation.", workflow.Id);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}