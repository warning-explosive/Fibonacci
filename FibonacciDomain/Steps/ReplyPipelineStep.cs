namespace FibonacciDomain.Steps
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
        
        public async Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(CalculationStoredEvent domainEvent)
        {
            var request = new FibonacciRequest
                          {
                              CalculationId = domainEvent.CalculatedEvent.CalculationId,
                              CurrentNumber = domainEvent.CalculatedEvent.NextNumber
                          };

            await _transmitter.RequestAsync<FibonacciRequest, AcknowledgementResponse>(request);
            
            return new[]
                   {
                       new ReplySentEvent(request)
                   };
        }
    }
}