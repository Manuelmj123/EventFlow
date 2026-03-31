using EventFlow.Application.Abstractions;
using EventFlow.Contracts.Events;
using EventFlow.Domain.Entities;
using EventFlow.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventFlow.Worker.Notifications;

public sealed class NotificationsWorker : BackgroundService
{
    private const string ProcessedQueueName = "eventflow.workflow.processed.q";
    private const string FailedQueueName = "eventflow.workflow.failed.q";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<NotificationsWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public NotificationsWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<Infrastructure.Options.RabbitMqOptions> rabbitMqOptions,
        ILogger<NotificationsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
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

        var processedConsumer = new EventingBasicConsumer(channel);
        processedConsumer.Received += async (_, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<WorkflowProcessedEvent>(body);

                if (message is null)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await HandleProcessedAsync(message, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notifications worker failed for processed event.");
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        };

        var failedConsumer = new EventingBasicConsumer(channel);
        failedConsumer.Received += async (_, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<WorkflowFailedEvent>(body);

                if (message is null)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await HandleFailedAsync(message, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notifications worker failed for failed event.");
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: ProcessedQueueName,
            autoAck: false,
            consumer: processedConsumer);

        channel.BasicConsume(
            queue: FailedQueueName,
            autoAck: false,
            consumer: failedConsumer);

        return Task.CompletedTask;
    }

    private async Task HandleProcessedAsync(
        WorkflowProcessedEvent message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var workflowRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var eventLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowEventLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var workflow = await workflowRepository.GetByIdAsync(message.WorkflowId, cancellationToken);

        if (workflow is null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in notifications processed handler.", message.WorkflowId);
            return;
        }

        workflow.MarkCompleted();
        await workflowRepository.UpdateAsync(workflow, cancellationToken);

        var log = new WorkflowEventLog(
            workflow.Id,
            "workflow.completed",
            "Consumed",
            JsonSerializer.Serialize(message),
            nameof(NotificationsWorker));

        await eventLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow {WorkflowId} completed notification handled.", workflow.Id);
    }

    private async Task HandleFailedAsync(
        WorkflowFailedEvent message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var workflowRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var eventLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowEventLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var workflow = await workflowRepository.GetByIdAsync(message.WorkflowId, cancellationToken);

        if (workflow is null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in notifications failed handler.", message.WorkflowId);
            return;
        }

        workflow.MarkFailed();
        await workflowRepository.UpdateAsync(workflow, cancellationToken);

        var log = new WorkflowEventLog(
            workflow.Id,
            "workflow.failed",
            "Consumed",
            JsonSerializer.Serialize(message),
            nameof(NotificationsWorker));

        await eventLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow {WorkflowId} failure notification handled.", workflow.Id);
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