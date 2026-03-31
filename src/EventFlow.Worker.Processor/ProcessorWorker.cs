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

namespace EventFlow.Worker.Processor;

public sealed class ProcessorWorker : BackgroundService
{
    private const string QueueName = "eventflow.workflow.validated.q";
    private const string SuccessRoutingKey = "workflow.processed";
    private const string FailureRoutingKey = "workflow.failed";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ILogger<ProcessorWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public ProcessorWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILogger<ProcessorWorker> logger)
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
                var message = JsonSerializer.Deserialize<WorkflowValidatedEvent>(body);

                if (message is null)
                {
                    _logger.LogWarning("Received null or invalid workflow.validated event.");
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await HandleMessageAsync(message, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processor worker failed processing message.");
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
        WorkflowValidatedEvent message,
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
            _logger.LogWarning("Workflow {WorkflowId} not found in processor.", message.WorkflowId);
            return;
        }

        try
        {
            await Task.Delay(500, cancellationToken);

            var resultJson = JsonSerializer.Serialize(new
            {
                processed = true,
                processedAtUtc = DateTime.UtcNow,
                worker = nameof(ProcessorWorker)
            });

            workflow.MarkProcessed();
            workflow.UpdatePayload(resultJson);

            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var processedEvent = new WorkflowProcessedEvent
            {
                WorkflowId = workflow.Id,
                CorrelationId = workflow.CorrelationId,
                EventName = SuccessRoutingKey,
                Name = workflow.Name,
                PayloadJson = message.PayloadJson,
                ResultJson = resultJson
            };

            var log = new WorkflowEventLog(
                workflow.Id,
                SuccessRoutingKey,
                "Published",
                JsonSerializer.Serialize(processedEvent),
                nameof(ProcessorWorker));

            await eventLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await messagePublisher.PublishAsync(processedEvent, SuccessRoutingKey, cancellationToken);

            _logger.LogInformation("Workflow {WorkflowId} processed.", workflow.Id);
        }
        catch (Exception ex)
        {
            workflow.MarkFailed();
            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var failedEvent = new WorkflowFailedEvent
            {
                WorkflowId = workflow.Id,
                CorrelationId = workflow.CorrelationId,
                EventName = FailureRoutingKey,
                Name = workflow.Name,
                PayloadJson = message.PayloadJson,
                ErrorMessage = ex.Message,
                FailedBy = nameof(ProcessorWorker)
            };

            var log = new WorkflowEventLog(
                workflow.Id,
                FailureRoutingKey,
                "Published",
                JsonSerializer.Serialize(failedEvent),
                nameof(ProcessorWorker));

            await eventLogRepository.AddAsync(log, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await messagePublisher.PublishAsync(failedEvent, FailureRoutingKey, cancellationToken);

            _logger.LogError(ex, "Workflow {WorkflowId} failed during processing.", workflow.Id);
        }
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