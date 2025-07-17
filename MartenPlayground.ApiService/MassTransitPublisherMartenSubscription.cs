using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using MassTransit;

namespace MartenPlayground.ApiService;

public class MassTransitPublisherMartenSubscription(IPublishEndpoint publishEndpoint) : ISubscription
{
    public Task<IChangeListener> ProcessEventsAsync(EventRange page, ISubscriptionController controller, IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting to process events from {page.SequenceFloor} to {page.SequenceCeiling}");
        foreach (var e in page.Events)
        {
            var type = e.EventType;

            // create a generic type for the event
            // var eventType = typeof(StreamedIntegrationEvent<>).MakeGenericType(type);

            Console.WriteLine($"Processing event of type {e.Data.GetType()} with ID {e.Id}");

            //// convert the event to the generic type and map properties
            //var streamedEvent = Activator.CreateInstance(eventType);
            //if (streamedEvent is StreamedIntegrationEvent<object> integrationEvent)
            //{
            //    integrationEvent.Id = e.Id;
            //    integrationEvent.Version = e.Version;
            //    integrationEvent.Sequence = e.Sequence;
            //    integrationEvent.Data = e.Data;
            //    integrationEvent.StreamId = e.StreamId;
            //    integrationEvent.StreamKey = e.StreamKey;
            //    integrationEvent.Timestamp = e.Timestamp;
            //    integrationEvent.TenantId = e.TenantId;
            //    integrationEvent.EventTypeName = e.EventTypeName;
            //    integrationEvent.DotNetTypeName = e.DotNetTypeName;
            //    integrationEvent.CausationId = e.CausationId;
            //    integrationEvent.CorrelationId = e.CorrelationId;
            //    integrationEvent.Headers = e.Headers;
            //    // Publish the event using MassTransit
            //    publishEndpoint.Publish(streamedEvent, cancellationToken);
            //}
        }

        // If you don't care about being signaled for
        return Task.FromResult(NullChangeListener.Instance);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}

public class StreamedIntegrationEvent<T>
{
    //
    // Summary:
    //     Unique identifier for the event. Uses a sequential Guid
    public Guid Id { get; set; }

    //
    // Summary:
    //     The version of the stream this event reflects. The place in the stream.
    public long Version { get; set; }

    //
    // Summary:
    //     The sequential order of this event in the entire event store
    public long Sequence { get; set; }

    //
    // Summary:
    //     The actual event data body
    public T Data { get; set; }

    //
    // Summary:
    //     If using Guid's for the stream identity, this will refer to the Stream's Id,
    //     otherwise it will always be Guid.Empty
    public Guid StreamId { get; set; }

    //
    // Summary:
    //     If using strings as the stream identifier, this will refer to the containing
    //     Stream's Id
    public string? StreamKey { get; set; }

    //
    // Summary:
    //     The UTC time that this event was originally captured
    public DateTimeOffset Timestamp { get; set; }

    //
    // Summary:
    //     If using multi-tenancy by tenant id
    public string TenantId { get; set; }

    //
    // Summary:
    //     Marten's type alias string for the Event type
    public string EventTypeName { get; set; }

    //
    // Summary:
    //     Marten's string representation of the event type in assembly qualified name
    public string DotNetTypeName { get; set; }

    //
    // Summary:
    //     Optional metadata describing the causation id
    public string? CausationId { get; set; }

    //
    // Summary:
    //     Optional metadata describing the correlation id
    public string? CorrelationId { get; set; }

    //
    // Summary:
    //     Optional user defined metadata values. This may be null.
    public Dictionary<string, object>? Headers { get; set; }
}