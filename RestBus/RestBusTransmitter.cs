namespace RestBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TransportApi;

    public class RestBusTransmitter : IBusTransmitter
    {
        private readonly HttpClient _client;
        
        private readonly Uri _uri;
        
        private readonly ICollection<IRestBusTransmitterFactory> _factories;

        public RestBusTransmitter(Uri uri,
                                  int millisecondsTimeout,
                                  IEnumerable<IRestBusTransmitterFactory> factories)
        {
            _uri = uri;
            _client = new HttpClient
                      {
                          Timeout = TimeSpan.FromMilliseconds(millisecondsTimeout),
                      };
            _factories = factories.Reverse().ToList(); // reverse for overrides
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            var postUri = GetPostUri<TRequest, TResponse>(_uri, request);

            try
            {
                using (var response = await _client.PostAsync(postUri, new StringContent(string.Empty)))
                {
                    response.EnsureSuccessStatusCode();
                }
                
                return ConstructResponse<TRequest, TResponse>(request);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException();
            }
        }

        private TResponse ConstructResponse<TRequest, TResponse>(TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            try
            {
                return _factories.OfType<IRestBusTransmitterGenericFactory<TRequest, TResponse>>()
                                 .First()
                                 .ResponseFactory(request);
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(typeof(TRequest).FullName);
            }
        }

        private Uri GetPostUri<TRequest, TResponse>(Uri baseUri, TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            try
            {
                return _factories.OfType<IRestBusTransmitterGenericFactory<TRequest, TResponse>>()
                                 .First()
                                 .PostUriFactory(baseUri, request);
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(typeof(TRequest).FullName);
            }
        }
    } 
}