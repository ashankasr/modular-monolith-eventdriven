namespace ModularMonolithEventDriven.Common.Domain.Primitives;

public abstract class Entity<TKey> : IEquatable<Entity<TKey>>
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TKey id)
    {
        Id = id;
    }

    protected Entity() { }

    public TKey Id { get; protected set; } = default!;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public bool Equals(Entity<TKey>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) =>
        obj is Entity<TKey> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) =>
        !Equals(left, right);
}
