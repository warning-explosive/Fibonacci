namespace TransportApi
{
    public interface IRequest<TResponse>
        where TResponse : IResponse
    {
    }
}