namespace TransportApi
{
    using System;
    using System.Threading.Tasks;

    public interface IBusTransmitter : IDisposable
    {
        Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse;
    }
}