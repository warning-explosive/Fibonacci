namespace EventBusApi
{
    public interface IEventBus
    {
        void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent;

        void PlaceError(IDomainEvent domainEvent, string error);

        bool TryDequeue(out IDomainEvent domainEvent);
    }
}
