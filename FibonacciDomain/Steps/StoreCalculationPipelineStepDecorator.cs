namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DomainEvents;
    using EventBusApi;

    public class StoreCalculationPipelineStepDecorator : IPipelineStep<CalculatedEvent>
    {
        private readonly IPipelineStep<CalculatedEvent> _decoratee;

        public StoreCalculationPipelineStepDecorator(IPipelineStep<CalculatedEvent> decoratee)
        {
            _decoratee = decoratee;
        }

        public Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(CalculatedEvent domainEvent)
        {
            Console.WriteLine($"Attempt to store: {domainEvent.CalculationId} | {domainEvent.NextNumber}");
            
            return _decoratee.HandleEvent(domainEvent)
                             .ContinueWith(prev =>
                                           {
                                               foreach (var stored in prev.Result.OfType<CalculationStoredEvent>())
                                               {
                                                   Console.WriteLine($"Stored: {stored.CalculatedEvent.CalculationId} | {stored.CalculatedEvent.NextNumber}");
                                               }

                                               return prev.Result;
                                           },
                                           TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted);
        }
    }
}