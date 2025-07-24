using JasperFx.Events;
using Marten.Events.Projections;

namespace MartenPlayground.ApiService.Domain;

public record EntityHistory(Guid Id, Guid EntityId, string Description);

public class EntityHistoryTransformation : EventProjection
{
    public EntityHistory Transform(IEvent<EntityCreated> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"Entity created with ID {input.Data.EntityId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<AsicExtractInitiated> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"ASIC extract initiated with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<RiskExtractInitiated> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"Risk extract initiated with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<RiskExtractReceived> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"Risk extract received with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<AsicExtractReceived> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"ASIC extract received with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<AsicExtractOrderCreated> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"ASIC extract order created with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }

    public EntityHistory Transform(IEvent<AsicExtractOrderCompleted> input)
    {
        return new EntityHistory(
            input.Id,
            input.Data.EntityId,
            $"ASIC extract order completed with ID {input.Data.ExtractId} at {input.Timestamp:O}"
        );
    }
}
