namespace FibonacciDomain.DomainEvents
{
    using System;
    using EventBusApi;

    [Serializable]
    [Priority(1)]
    public class CalculationStoredEvent : IDomainEvent
    {
        public CalculationStoredEvent(CalculatedEvent calculatedEvent)
        {
            CalculatedEvent = calculatedEvent;
        }

        public CalculatedEvent CalculatedEvent { get; }
    }
}