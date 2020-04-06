namespace EventBusApi
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPipelineStep<TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
        Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(TDomainEvent domainEvent);
    }
}