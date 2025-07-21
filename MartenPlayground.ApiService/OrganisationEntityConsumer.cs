using MartenPlayground.ApiService.Application;
using MartenPlayground.ApiService.Domain;
using MassTransit;

namespace MartenPlayground.ApiService;

// TODO: source generator?
public class OrganisationEntityConsumer :
    IConsumer<StreamIntegrationEvent<RiskExtractInitiated>>,
    IConsumer<StreamIntegrationEvent<AsicExtractInitiated>>
{
    public async Task Consume(ConsumeContext<StreamIntegrationEvent<AsicExtractInitiated>> context)
    {
        await HandleAsync(context);
    }

    public async Task Consume(ConsumeContext<StreamIntegrationEvent<RiskExtractInitiated>> context)
    {
        await HandleAsync(context);
    }

    private static async Task HandleAsync<TEvent>(
        ConsumeContext<StreamIntegrationEvent<TEvent>> context) where TEvent : class
    {
        // TODO: replace with a mediator
        var handler = context.GetServiceOrCreateInstance<IStreamIntegrationEventHandler<TEvent>>();
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}
