namespace EventBusApi
{
    using System.Collections.Generic;

    public interface IPipelineStep<TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
        IReadOnlyCollection<IDomainEvent> HandleEvent(TDomainEvent domainEvent);
    }
}