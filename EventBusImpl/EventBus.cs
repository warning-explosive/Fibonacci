namespace EventBusImpl
{
    using System.Collections.Concurrent;
    using EventBusApi;

    public class EventBus : IEventBus
    {
        private readonly ConcurrentQueue<IDomainEvent> _queue = new ConcurrentQueue<IDomainEvent>();

        public void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent
        {
            _queue.Enqueue(domainEvent);
        }

        public bool TryDequeue(out IDomainEvent domainEvent)
        {
            return _queue.TryDequeue(out domainEvent);
        }
    }
}
