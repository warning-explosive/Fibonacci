namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DomainEvents;
    using EventBusApi;

    public class ReplyPipelineStepDecorator : IPipelineStep<CalculationStoredEvent>
    {
        private readonly IPipelineStep<CalculationStoredEvent> _decoratee;

        public ReplyPipelineStepDecorator(IPipelineStep<CalculationStoredEvent> decoratee)
        {
            _decoratee = decoratee;
        }

        public Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(CalculationStoredEvent domainEvent)
        {
            Console.WriteLine($"Attempt to send: {domainEvent.CalculatedEvent.CalculationId} | {domainEvent.CalculatedEvent.NextNumber}");
            
            return _decoratee.HandleEvent(domainEvent)
                             .ContinueWith(prev =>
                                           {
                                               foreach (var sent in prev.Result.OfType<ReplySentEvent>())
                                               {
                                                   Console.WriteLine($"Sent: {sent.Request.CalculationId} | {sent.Request.CurrentNumber}");
                                               }

                                               return prev.Result;
                                           },
                                           TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted);
            
            
        }
    }
}