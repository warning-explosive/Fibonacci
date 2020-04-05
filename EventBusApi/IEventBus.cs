namespace EventBusApi
{
    public interface IEventBus
    {
        void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent;

        bool TryDequeue(out IDomainEvent domainEvent);
    }
}
