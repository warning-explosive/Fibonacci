namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomainEvents;
    using EventBusApi;

    public class ReplyPipelineStepDecorator : IPipelineStep<CalculationStoredEvent>
    {
        private readonly IPipelineStep<CalculationStoredEvent> _decoratee;

        public ReplyPipelineStepDecorator(IPipelineStep<CalculationStoredEvent> decoratee)
        {
            _decoratee = decoratee;
        }

        public IReadOnlyCollection<IDomainEvent> HandleEvent(CalculationStoredEvent domainEvent)
        {
            Console.WriteLine($"Attempt to send: {domainEvent.CalculatedEvent.CalculationId} | {domainEvent.CalculatedEvent.NextNumber}");
            
            var events = _decoratee.HandleEvent(domainEvent);
            
            foreach (var sent in events.OfType<ReplySentEvent>())
            {
                Console.WriteLine($"Sent: {sent.Request.CalculationId} | {sent.Request.CurrentNumber}");
            }

            return events;
        }
    }
}