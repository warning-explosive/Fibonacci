namespace EventBusImpl
{
    using System.Collections.Concurrent;
    using System.Linq;
    using EventBusApi;

    // TODO: concurrent priority queue via binary heap
    public class DummyEventBusWithPriority : IEventBusWithPriority
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<IDomainEvent>> _storage = new ConcurrentDictionary<int, ConcurrentQueue<IDomainEvent>>();
        
        public void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent, int priority)
            where TDomainEvent : IDomainEvent
        {
            _storage.GetOrAdd(priority, _ => new ConcurrentQueue<IDomainEvent>())
                    .Enqueue(domainEvent);
        }

        public bool TryDequeue(out IDomainEvent domainEvent)
        {
            domainEvent = null;

            return _storage.OrderByDescending(z => z.Key)
                           .FirstOrDefault(z => z.Value.Any())
                           .Value
                          ?.TryDequeue(out domainEvent) ?? false;
        }
    }
}