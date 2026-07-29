namespace SharedKernel;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void RaiseDomainEvent(IDomainEvent domainEvent);
}

public interface IAggregateRootChangeTracker
{
    void Enqueue(IAggregateRoot  aggregateRoot);
    IEnumerable<IAggregateRoot> Dequeue();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    
    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

public abstract class AggregateRoot : AggregateRoot<Guid>;

public sealed class AggregateRootChangeTracker : IAggregateRootChangeTracker
{
    private readonly Queue<IAggregateRoot> _tracker = new();

    public void Enqueue(IAggregateRoot aggregateRoot)
        => _tracker.Enqueue(aggregateRoot);

    public IEnumerable<IAggregateRoot> Dequeue()
    {
        while (_tracker.Count > 0)
            yield return _tracker.Dequeue();
    }
}
