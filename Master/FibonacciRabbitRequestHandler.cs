namespace Master
{
    using EventBusApi;
    using FibonacciDomain;
    using FibonacciDomain.DomainEvents;
    using TransportApi;

    public class FibonacciRabbitRequestHandler : IRequestHandler<FibonacciRequest, AcknowledgementResponse>
    {
        private readonly IEventBus _eventBus;
        
        public FibonacciRabbitRequestHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public AcknowledgementResponse HandleRequest(FibonacciRequest request)
        {
            _eventBus.PlaceEvent(new RequestReceivedEvent<FibonacciRequest>(request));
            
            return new AcknowledgementResponse();
        }
    }
}