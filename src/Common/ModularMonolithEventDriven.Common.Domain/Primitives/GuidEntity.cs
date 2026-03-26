namespace ModularMonolithEventDriven.Common.Domain.Primitives;

public abstract class GuidEntity : Entity<Guid>
{
    protected GuidEntity(Guid id) : base(id) { }
    protected GuidEntity() { }
}
