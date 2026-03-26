namespace ModularMonolithEventDriven.Common.Domain.Primitives;

public abstract class AuditableEntity<TKey> : Entity<TKey>
    where TKey : notnull
{
    protected AuditableEntity(TKey id) : base(id) { }
    protected AuditableEntity() { }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
