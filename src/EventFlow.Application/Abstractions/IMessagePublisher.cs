using EventFlow.Contracts.Common;

namespace EventFlow.Application.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(
        T integrationEvent,
        string routingKey,
        CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
}