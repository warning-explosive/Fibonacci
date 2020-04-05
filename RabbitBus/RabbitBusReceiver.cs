namespace RabbitBasics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EasyNetQ;
    using TransportApi;

    public class RabbitBusReceiver : IBusReceiver
    {
        private readonly IConventions _conventions;
        
        private readonly IBus _bus;
        
        private readonly ICollection<IDisposable> _disposables = new List<IDisposable>(); 
        
        public RabbitBusReceiver(RabbitConnectionConfig connectionConfig, IConventions conventions)
        {
            _conventions = conventions;
            _bus = RabbitHutch.CreateBus(connectionConfig.ToString(), registration => registration.Register(_conventions));
        }

        public void Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            var disposable = _bus.RespondAsync<TRequest, TResponse>(request => Task.Factory.StartNew(() => handler.HandleRequest(request), TaskCreationOptions.DenyChildAttach));
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            
            _bus?.Dispose();
        }
    }
}