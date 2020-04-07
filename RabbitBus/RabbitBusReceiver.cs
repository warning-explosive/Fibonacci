namespace RabbitBasics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EasyNetQ;
    using EasyNetQ.Topology;
    using TransportApi;

    public class RabbitBusReceiver : IBusReceiver
    {
        private readonly IConventions _conventions;
        
        private readonly IBus _bus;
        
        private readonly ICollection<Action> _disposables = new List<Action>(); 
        
        public RabbitBusReceiver(RabbitConnectionConfig connectionConfig, IConventions conventions)
        {
            _conventions = conventions;
            _bus = RabbitHutch.CreateBus(connectionConfig.ToString(), registration => registration.Register(_conventions));
        }

        public void Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            _bus.RespondAsync<TRequest, TResponse>(request => Task.Factory.StartNew(() => handler.HandleRequest(request), TaskCreationOptions.AttachedToParent));
            
            // get reference to respond queue
            var queue = _bus.Advanced.QueueDeclare(_conventions.RpcRoutingKeyNamingConvention.Invoke(typeof(TRequest)));
            _disposables.Add(() => DisposeRespondQueue(queue));
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Invoke();
            }
            
            _bus.Dispose();
        }
        
        private void DisposeRespondQueue(IQueue queue)
        {
            _bus.Advanced.QueuePurge(queue);
            _bus.Advanced.QueueDelete(queue);
        }
    }
}