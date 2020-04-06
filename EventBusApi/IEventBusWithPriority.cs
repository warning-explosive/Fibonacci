namespace EventBusApi
{
    public interface IEventBusWithPriority
    {
        void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent, int priority)
            where TDomainEvent : IDomainEvent;

        bool TryDequeue(out IDomainEvent domainEvent);
    }
}