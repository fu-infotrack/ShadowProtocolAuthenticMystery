using MartenPlayground.ApiService.Domain;

namespace MartenPlayground.ApiService.Application;

public class RiskExtractInitiatedHandler(
    ILogger<RiskExtractInitiatedHandler> logger)
    : IStreamIntegrationEventHandler<RiskExtractInitiated>
{
    public async Task HandleAsync(StreamIntegrationEvent<RiskExtractInitiated> integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
                "Received RiskExtractInitiated event with ID {Id} and Version {Version}",
                integrationEvent.Id, integrationEvent.Version);

        await Task.Delay(1000, cancellationToken); // Simulate calling risk ds
    }
}
