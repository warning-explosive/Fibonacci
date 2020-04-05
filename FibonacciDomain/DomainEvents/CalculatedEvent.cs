namespace FibonacciDomain.DomainEvents
{
    using System;
    using EventBusApi;

    [Serializable]
    public class CalculatedEvent : IDomainEvent
    {
        public CalculatedEvent(Guid calculationId, long nextNumber)
        {
            CalculationId = calculationId;
            NextNumber = nextNumber;
        }

        public Guid CalculationId { get; }

        public long NextNumber { get; }
    }
}