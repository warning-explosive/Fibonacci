namespace FibonacciDomain
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using EventBusApi;

    public class HybridEventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, int> _cache; 
        
        private readonly IEventBusWithPriority _eventBusWithPriority;
        
        private readonly IEventBus _decoratee;

        public HybridEventBus(IEventBusWithPriority eventBusWithPriority, IEventBus decoratee)
        {
            _eventBusWithPriority = eventBusWithPriority;
            _decoratee = decoratee;
            _cache = new ConcurrentDictionary<Type, int>();
        }

        public void PlaceEvent<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent
        {
            var priority = _cache.GetOrAdd(domainEvent.GetType(), type => type.GetCustomAttribute<PriorityAttribute>()?.PriorityLevel ?? int.MaxValue);
            
            _eventBusWithPriority.PlaceEvent(domainEvent, priority);
        }

        public void PlaceError(IDomainEvent domainEvent, string error)
        {
            _decoratee.PlaceError(domainEvent, error);
        }

        public bool TryDequeue(out IDomainEvent domainEvent)
        {
            return _eventBusWithPriority.TryDequeue(out domainEvent);
        }
    }
}