namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DomainEvents;
    using EventBusApi;

    public class CalculationPipelineStepDecorator : IPipelineStep<RequestReceivedEvent<FibonacciRequest>>
    {
        private readonly IPipelineStep<RequestReceivedEvent<FibonacciRequest>> _decoratee;

        public CalculationPipelineStepDecorator(IPipelineStep<RequestReceivedEvent<FibonacciRequest>> decoratee)
        {
            _decoratee = decoratee;
        }
        
        public Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(RequestReceivedEvent<FibonacciRequest> domainEvent)
        {
            Console.WriteLine($"Calculation: {domainEvent.Request.CalculationId} | {domainEvent.Request.CurrentNumber}");
            
            return _decoratee.HandleEvent(domainEvent)
                             .ContinueWith(prev =>
                                           {
                                               foreach (var calculated in prev.Result.OfType<CalculatedEvent>())
                                               {
                                                   Console.WriteLine($"Calculated: {calculated.CalculationId} | {calculated.NextNumber}");
                                               }
                                    
                                               return prev.Result;
                                           },
                                           TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted);
        }
    }
}