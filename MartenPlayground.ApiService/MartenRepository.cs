/*
 * ported from https://github.com/oskardudycz/EventSourcing.NetCore/blob/main/Core/Aggregates/Aggregate.cs
 */

using Marten;

namespace MartenPlayground.ApiService;

public interface IMartenRepository<T> where T : class
{
    Task<T?> Find(Guid id, CancellationToken cancellationToken);
    Task<long> Add(Guid id, T aggregate, CancellationToken cancellationToken = default);
    Task<long> Update(Guid id, T aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default);
    Task<long> GetAndUpdate(Guid id, Action<T> action, long? expectedVersion = null, CancellationToken cancellationToken = default);
}

public class MartenRepository<TAggregate>(IDocumentSession documentSession) : IMartenRepository<TAggregate>
    where TAggregate : class, IAggregate
{
    public Task<TAggregate?> Find(Guid id, CancellationToken ct) =>
        documentSession.Events.AggregateStreamAsync<TAggregate>(id, token: ct);

    public async Task<long> Add(Guid id, TAggregate aggregate, CancellationToken ct = default)
    {
        var events = aggregate.DequeueUncommittedEvents();

        documentSession.Events.StartStream<Aggregate>(
            id,
            events
        );

        await documentSession.SaveChangesAsync(ct).ConfigureAwait(false);

        return events.Length;
    }

    public async Task<long> Update(Guid id, TAggregate aggregate, long? expectedVersion = null, CancellationToken ct = default)
    {
        var events = aggregate.DequeueUncommittedEvents();

        var nextVersion = (expectedVersion ?? aggregate.Version) + events.Length;

        documentSession.Events.Append(
            id,
            nextVersion,
            events
        );

        await documentSession.SaveChangesAsync(ct).ConfigureAwait(false);

        return nextVersion;
    }

    public async Task<long> GetAndUpdate(
        Guid id,
        Action<TAggregate> action,
        long? expectedVersion = null,
        CancellationToken ct = default
    )
    {
        var stream = await documentSession.Events.FetchForWriting<TAggregate>(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Aggregate with ID {id} not found.");
        var entity = stream.Aggregate;

        action(entity);

        return await Update(id, entity, expectedVersion, ct).ConfigureAwait(false);
    }
}

public interface IAggregate : IProjection
{
    int Version { get; }

    object[] DequeueUncommittedEvents();
}

public interface IProjection
{
    void Apply(object @event);
}

public interface IProjection<in TEvent> : IProjection where TEvent : class
{
    void Apply(TEvent @event);

    void IProjection.Apply(object @event)
    {
        if (@event is TEvent typedEvent)
            Apply(typedEvent);
    }
}

public interface IAggregate<in TEvent> : IAggregate, IProjection<TEvent> where TEvent : class;

public abstract class Aggregate : Aggregate<object, Guid>;

public abstract class Aggregate<TEvent> : Aggregate<TEvent, Guid> where TEvent : class;

public abstract class Aggregate<TEvent, TId> : IAggregate<TEvent>
    where TEvent : class
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public int Version { get; protected set; }

    [NonSerialized] private readonly Queue<TEvent> uncommittedEvents = new();

    public virtual void Apply(TEvent @event) { }

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = uncommittedEvents.Cast<object>().ToArray();

        uncommittedEvents.Clear();

        return dequeuedEvents;
    }

    protected void Enqueue(TEvent @event)
    {
        uncommittedEvents.Enqueue(@event);
        Apply(@event);
    }
}