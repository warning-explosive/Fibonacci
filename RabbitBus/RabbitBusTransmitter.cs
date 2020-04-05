namespace RabbitBasics
{
    using System.Threading.Tasks;
    using EasyNetQ;
    using TransportApi;

    public class RabbitBusTransmitter : IBusTransmitter
    {
        private readonly IConventions _conventions;
        
        private readonly IBus _bus;

        public RabbitBusTransmitter(RabbitConnectionConfig connectionConfig, IConventions conventions)
        {
            _conventions = conventions;
            _bus = RabbitHutch.CreateBus(connectionConfig.ToString(), registration => registration.Register(_conventions));
        }

        public void Dispose()
        {
            _bus?.Dispose();
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            return _bus.RequestAsync<TRequest, TResponse>(request);
        }
    }
}