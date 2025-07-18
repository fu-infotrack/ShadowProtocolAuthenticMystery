using System.Security.Cryptography;
using MartenPlayground.ApiService.Domain;
using MassTransit;

namespace MartenPlayground.ApiService
{
    public class OrganisationEntityConsumer(ILogger<OrganisationEntityConsumer> logger) :
        IConsumer<IntegrationEvent<RiskExtractInitiated>>,
        IConsumer<IntegrationEvent<AsicExtractInitiated>>
    {
        public async Task Consume(ConsumeContext<IntegrationEvent<AsicExtractInitiated>> context)
        {
            // TODO: in the very rare case of duplicated message received,
            // how can we ensure that we do not create a new order?
            // Option 1: do nothing but relying on the assumption that ds calls are idempotent
            // Option 2: lock the process with the extract id and validate against the latest state

            logger.LogInformation(
                "Received AsicExtractInitiated event with ID {Id} and Version {Version}",
                context.Message.Id, context.Message.Version);

            var repo = context.GetServiceOrCreateInstance<IMartenRepository<OrganisationEntity>>();

            var @initiatedEvent = context.Message;

            await Task.Delay(1000); // Simulate calling ASIC ds
            var orderId = RandomNumberGenerator.GetInt32(1000);

            var entity = await repo.GetAndUpdate(
                context.Message.StreamId,
                e => e.CreateAsicExtractOrder(new AsicExtractOrderCreated(@initiatedEvent.Data.Id, orderId)),
                cancellationToken: context.CancellationToken);
        }

        public async Task Consume(ConsumeContext<IntegrationEvent<RiskExtractInitiated>> context)
        {
            logger.LogInformation(
                "Received RiskExtractInitiated event with ID {Id} and Version {Version}",
                context.Message.Id, context.Message.Version);

            await Task.Delay(1000); // Simulate calling risk ds
        }
    }
}
