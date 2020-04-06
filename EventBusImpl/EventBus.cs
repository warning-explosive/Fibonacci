namespace EventBusImpl
{
    using System.Collections.Concurrent;
    using EventBusApi;

    public class EventBus : IEventBus
    {
        private readonly ConcurrentQueue<IDomainEvent> _queue = new ConcurrentQueue<IDomainEvent>();
        
        private readonly ConcurrentBag<(IDomainEvent domainEvent, string error)> _errorBag = new ConcurrentBag<(IDomainEvent domainEvent, string error)>();

        public void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent
        {
            _queue.Enqueue(domainEvent);
        }

        public void PlaceError(IDomainEvent domainEvent, string error)
        {
            _errorBag.Add((domainEvent, error));
        }

        public bool TryDequeue(out IDomainEvent domainEvent)
        {
            return _queue.TryDequeue(out domainEvent);
        }
    }
}
