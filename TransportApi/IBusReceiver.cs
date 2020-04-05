namespace TransportApi
{
    using System;

    public interface IBusReceiver : IDisposable
    {
        void Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse;
    }
}