namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomainEvents;
    using EventBusApi;

    public class StoreCalculationPipelineStepDecorator : IPipelineStep<CalculatedEvent>
    {
        private readonly IPipelineStep<CalculatedEvent> _decoratee;

        public StoreCalculationPipelineStepDecorator(IPipelineStep<CalculatedEvent> decoratee)
        {
            _decoratee = decoratee;
        }

        public IReadOnlyCollection<IDomainEvent> HandleEvent(CalculatedEvent domainEvent)
        {
            Console.WriteLine($"Attempt to store: {domainEvent.CalculationId} | {domainEvent.NextNumber}");
            
            var events = _decoratee.HandleEvent(domainEvent);
            
            foreach (var stored in events.OfType<CalculationStoredEvent>())
            {
                Console.WriteLine($"Stored: {stored.CalculatedEvent.CalculationId} | {stored.CalculatedEvent.NextNumber}");
            }

            return events;
        }
    }
}