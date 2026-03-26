namespace ModularMonolithEventDriven.Common.Domain.Primitives;

public abstract class AuditableGuidEntity : AuditableEntity<Guid>
{
    protected AuditableGuidEntity(Guid id) : base(id) { }
    protected AuditableGuidEntity() { }
}
