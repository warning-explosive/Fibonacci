namespace FibonacciDomain.Steps
{
    using System.Collections.Generic;
    using Calculation;
    using DomainEvents;
    using EventBusApi;

    public class StoreCalculationPipelineStep : IPipelineStep<CalculatedEvent>
    {
        private readonly IFibonacciStorage _storage;

        public StoreCalculationPipelineStep(IFibonacciStorage storage)
        {
            _storage = storage;
        }

        public IReadOnlyCollection<IDomainEvent> HandleEvent(CalculatedEvent domainEvent)
        {
            _storage.Upsert(domainEvent.CalculationId, domainEvent.NextNumber);
            
            return new[]
                   {
                       new CalculationStoredEvent(domainEvent)
                   };
        }
    }
}