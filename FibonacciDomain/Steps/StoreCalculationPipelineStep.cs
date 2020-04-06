namespace FibonacciDomain.Steps
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
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

        public Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(CalculatedEvent domainEvent)
        {
            _storage.Upsert(domainEvent.CalculationId, domainEvent.NextNumber);

            IReadOnlyCollection<IDomainEvent> events = new List<IDomainEvent> { new CalculationStoredEvent(domainEvent) };
            
            return Task.FromResult(events);
        }
    }
}