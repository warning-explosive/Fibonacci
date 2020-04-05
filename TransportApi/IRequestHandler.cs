namespace TransportApi
{
    public interface IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        TResponse HandleRequest(TRequest request);
    }
}