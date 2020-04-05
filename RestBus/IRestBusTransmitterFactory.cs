namespace RestBus
{
    using System;
    using TransportApi;

    public interface IRestBusTransmitterGenericFactory<TRequest, TResponse> : IRestBusTransmitterFactory
        where TRequest : class, IRequest<TResponse>
        where TResponse : class, IResponse
    {
        TResponse ResponseFactory(TRequest request);

        Uri PostUriFactory(Uri baseUri, TRequest request);
    }

    public interface IRestBusTransmitterFactory
    {
    }
}