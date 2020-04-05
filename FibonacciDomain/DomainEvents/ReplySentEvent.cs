namespace FibonacciDomain.DomainEvents
{
    using System;
    using EventBusApi;

    [Serializable]
    public class ReplySentEvent : IDomainEvent
    {
        public ReplySentEvent(FibonacciRequest request)
        {
            Request = request;
        }

        public FibonacciRequest Request { get; }
    }
}