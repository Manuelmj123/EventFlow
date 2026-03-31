using EventFlow.Application.Abstractions;
using EventFlow.Contracts.Common;
using EventFlow.Domain.Entities;
using EventFlow.Infrastructure.Messaging;
using EventFlow.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventFlow.Worker.Audit;

public sealed class AuditWorker : BackgroundService
{
    private const string QueueName = "eventflow.audit.q";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<AuditWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public AuditWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILogger<AuditWorker> logger)
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
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (_, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<AuditIntegrationEnvelope>(body);

                if (message is null || message.WorkflowId == Guid.Empty)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await HandleMessageAsync(body, message, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit worker failed processing message.");
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(
        string rawBody,
        AuditIntegrationEnvelope message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var eventLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowEventLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var log = new WorkflowEventLog(
            message.WorkflowId,
            string.IsNullOrWhiteSpace(message.EventName) ? "unknown" : message.EventName,
            "Consumed",
            rawBody,
            nameof(AuditWorker));

        await eventLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit event recorded for workflow {WorkflowId} with event {EventName}.",
            message.WorkflowId,
            message.EventName);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }

    private sealed class AuditIntegrationEnvelope : IntegrationEvent
    {
    }
}