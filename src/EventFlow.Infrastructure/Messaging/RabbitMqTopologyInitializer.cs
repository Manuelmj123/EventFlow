using EventFlow.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventFlow.Infrastructure.Messaging;

public sealed class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;

    public RabbitMqTopologyInitializer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public Task InitializeAsync()
    {
        var factory = _connectionFactory.Create();

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: "eventflow.workflow.started.q",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: "eventflow.workflow.validated.q",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: "eventflow.workflow.processed.q",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: "eventflow.workflow.failed.q",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: "eventflow.audit.q",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: "eventflow.workflow.started.q",
            exchange: _options.ExchangeName,
            routingKey: "workflow.started");

        channel.QueueBind(
            queue: "eventflow.workflow.validated.q",
            exchange: _options.ExchangeName,
            routingKey: "workflow.validated");

        channel.QueueBind(
            queue: "eventflow.workflow.processed.q",
            exchange: _options.ExchangeName,
            routingKey: "workflow.processed");

        channel.QueueBind(
            queue: "eventflow.workflow.failed.q",
            exchange: _options.ExchangeName,
            routingKey: "workflow.failed");

        channel.QueueBind(
            queue: "eventflow.audit.q",
            exchange: _options.ExchangeName,
            routingKey: "workflow.*");

        return Task.CompletedTask;
    }
}