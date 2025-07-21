namespace MartenPlayground.ApiService.Application;

public interface IStreamIntegrationEventHandler<T>
{
    Task HandleAsync(StreamIntegrationEvent<T> integrationEvent, CancellationToken cancellationToken);
}
