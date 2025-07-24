using System.Security.Cryptography;
using MartenPlayground.ApiService.Domain;

namespace MartenPlayground.ApiService.Application;

public class AsicExtractInitiatedHandler(
    ILogger<AsicExtractInitiatedHandler> logger,
    IMartenRepository<OrganisationEntity> repository)
    : IStreamIntegrationEventHandler<AsicExtractInitiated>
{
    public async Task HandleAsync(StreamIntegrationEvent<AsicExtractInitiated> integrationEvent, CancellationToken cancellationToken = default)
    {
        // TODO: in the very rare case of duplicated message received,
        // how can we ensure that we do not create a new order?
        // Option 1: do nothing but relying on the assumption that ds calls are idempotent
        // Option 2: lock the process with the extract id and validate against the latest state

        logger.LogInformation(
                "Received AsicExtractInitiated event with ID {Id} and Version {Version}",
                integrationEvent.Id, integrationEvent.Version);

        await Task.Delay(1000, cancellationToken); // Simulate calling ASIC ds
        var orderId = RandomNumberGenerator.GetInt32(1000);

        var entity = await repository.GetAndUpdate(
            integrationEvent.StreamId,
            e => e.CreateAsicExtractOrder(new AsicExtractOrderCreated(integrationEvent.Data.EntityId, integrationEvent.Data.ExtractId, orderId)),
            cancellationToken: cancellationToken);
    }
}
