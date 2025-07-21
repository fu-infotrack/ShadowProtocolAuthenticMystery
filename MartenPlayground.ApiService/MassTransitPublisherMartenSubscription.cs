using System.Reflection;
using System.Text.Json.Serialization;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using MassTransit;

namespace MartenPlayground.ApiService;

public class MassTransitPublisherMartenSubscription(
    ILogger<MassTransitPublisherMartenSubscription> logger,
    IPublishEndpoint publishEndpoint)
    : ISubscription
{
    public async Task<IChangeListener> ProcessEventsAsync(EventRange page, ISubscriptionController controller, IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting to process events from {SequenceFloor} to {SequenceCeiling}", page.SequenceFloor, page.SequenceCeiling);
        foreach (var e in page.Events)
        {
            object integrationEvent = CreateIntegrationEventInstance(e);
            await publishEndpoint.Publish(integrationEvent, cancellationToken);
            logger.LogInformation("Published event of type {DataType} with ID {Id}", e.Data.GetType(), e.Id);
        }

        return NullChangeListener.Instance;
    }

    private static object CreateIntegrationEventInstance(IEvent e)
    {
        var createMethod = GetOrCreateCreateMethod(e.EventType);
        var integrationEvent = createMethod.Invoke(null, [e])!;

        return integrationEvent;
    }

    private static readonly Dictionary<Type, MethodInfo> _createMethodCache = [];

    private static MethodInfo GetOrCreateCreateMethod(Type type)
    {
        if (!_createMethodCache.TryGetValue(type, out var result))
        {
            var integrationEventType = typeof(StreamIntegrationEvent<>).MakeGenericType(type);
            result = integrationEventType.GetMethod(
                "Create",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
            _createMethodCache[type] = result;
        }
        return result;
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}

public class StreamIntegrationEvent<T>
{
    [JsonConstructor]
    protected StreamIntegrationEvent(
        Guid id,
        long version,
        long sequence,
        T data,
        Guid streamId,
        string? streamKey,
        DateTimeOffset timestamp,
        string tenantId,
        string eventTypeName,
        string dotNetTypeName,
        string? causationId,
        string? correlationId,
        Dictionary<string, object>? headers
        )
    {
        Id = id;
        Version = version;
        Sequence = sequence;
        Data = data;
        StreamId = streamId;
        StreamKey = streamKey;
        Timestamp = timestamp;
        TenantId = tenantId;
        EventTypeName = eventTypeName;
        DotNetTypeName = dotNetTypeName;
        CausationId = causationId;
        CorrelationId = correlationId;
        Headers = headers;
    }

    public static StreamIntegrationEvent<T> Create(IEvent @event)
    {
        return new StreamIntegrationEvent<T>(
            id: @event.Id,
            version: @event.Version,
            sequence: @event.Sequence,
            data: (T)@event.Data,
            streamId: @event.StreamId,
            streamKey: @event.StreamKey,
            timestamp: @event.Timestamp,
            tenantId: @event.TenantId,
            eventTypeName: @event.EventTypeName,
            dotNetTypeName: @event.DotNetTypeName,
            causationId: @event.CausationId,
            correlationId: @event.CorrelationId,
            headers: @event.Headers ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        );
    }

    //
    // Summary:
    //     Unique identifier for the event. Uses a sequential Guid
    public Guid Id { get; protected set; }

    //
    // Summary:
    //     The version of the stream this event reflects. The place in the stream.
    public long Version { get; protected set; }

    //
    // Summary:
    //     The sequential order of this event in the entire event store
    public long Sequence { get; protected set; }

    //
    // Summary:
    //     The actual event data body
    public T Data { get; protected set; }

    //
    // Summary:
    //     If using Guid's for the stream identity, this will refer to the Stream's Id,
    //     otherwise it will always be Guid.Empty
    public Guid StreamId { get; protected set; }

    //
    // Summary:
    //     If using strings as the stream identifier, this will refer to the containing
    //     Stream's Id
    public string? StreamKey { get; protected set; }

    //
    // Summary:
    //     The UTC time that this event was originally captured
    public DateTimeOffset Timestamp { get; protected set; }

    //
    // Summary:
    //     If using multi-tenancy by tenant id
    public string TenantId { get; protected set; }

    //
    // Summary:
    //     Marten's type alias string for the Event type
    public string EventTypeName { get; protected set; }

    //
    // Summary:
    //     Marten's string representation of the event type in assembly qualified name
    public string DotNetTypeName { get; protected set; }

    //
    // Summary:
    //     Optional metadata describing the causation id
    public string? CausationId { get; protected set; }

    //
    // Summary:
    //     Optional metadata describing the correlation id
    public string? CorrelationId { get; protected set; }

    //
    // Summary:
    //     Optional user defined metadata values. This may be null.
    public Dictionary<string, object>? Headers { get; protected set; }
}