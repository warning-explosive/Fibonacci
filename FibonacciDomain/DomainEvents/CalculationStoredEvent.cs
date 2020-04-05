namespace FibonacciDomain.DomainEvents
{
    using System;
    using EventBusApi;

    [Serializable]
    public class CalculationStoredEvent : IDomainEvent
    {
        public CalculationStoredEvent(CalculatedEvent calculatedEvent)
        {
            CalculatedEvent = calculatedEvent;
        }

        public CalculatedEvent CalculatedEvent { get; }
    }
}