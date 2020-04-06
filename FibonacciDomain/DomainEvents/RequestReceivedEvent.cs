namespace FibonacciDomain.DomainEvents
{
    using System;
    using EventBusApi;

    [Serializable]
    [Priority(0)]
    public class RequestReceivedEvent<TRequest> : IDomainEvent
    {
        public RequestReceivedEvent(TRequest request)
        {
            Request = request;
        }

        public TRequest Request { get; }
    }
}