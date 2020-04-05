namespace FibonacciDomain.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomainEvents;
    using EventBusApi;
    using TransportApi;

    public class ReplyPipelineStep : IPipelineStep<CalculationStoredEvent>
    {
        private readonly IBusTransmitter _transmitter;

        public ReplyPipelineStep(IBusTransmitter transmitter)
        {
            _transmitter = transmitter;
        }
        
        public IReadOnlyCollection<IDomainEvent> HandleEvent(CalculationStoredEvent domainEvent)
        {
            var request = new FibonacciRequest
                          {
                              CalculationId = domainEvent.CalculatedEvent.CalculationId,
                              CurrentNumber = domainEvent.CalculatedEvent.NextNumber
                          };

            try
            {
                _transmitter.RequestAsync<FibonacciRequest, AcknowledgementResponse>(request).Wait(); // TODO: remove block
            }
            catch (TimeoutException)
            {
                return Enumerable.Empty<IDomainEvent>().ToList();
            }
            
            return new[]
                   {
                       new ReplySentEvent(request)
                   };
        }
    }
}