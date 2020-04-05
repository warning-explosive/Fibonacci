namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomainEvents;
    using EventBusApi;

    public class CalculationPipelineStepDecorator : IPipelineStep<RequestReceivedEvent<FibonacciRequest>>
    {
        private readonly IPipelineStep<RequestReceivedEvent<FibonacciRequest>> _decoratee;

        public CalculationPipelineStepDecorator(IPipelineStep<RequestReceivedEvent<FibonacciRequest>> decoratee)
        {
            _decoratee = decoratee;
        }
        
        public IReadOnlyCollection<IDomainEvent> HandleEvent(RequestReceivedEvent<FibonacciRequest> domainEvent)
        {
            Console.WriteLine($"Calculation: {domainEvent.Request.CalculationId} | {domainEvent.Request.CurrentNumber}");
            
            var events = _decoratee.HandleEvent(domainEvent);

            foreach (var calculated in events.OfType<CalculatedEvent>())
            {
                Console.WriteLine($"Calculated: {calculated.CalculationId} | {calculated.NextNumber}");
            }

            return events;
        }
    }
}