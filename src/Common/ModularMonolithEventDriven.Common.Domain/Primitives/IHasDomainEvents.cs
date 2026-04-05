namespace ModularMonolithEventDriven.Common.Domain.Primitives;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
