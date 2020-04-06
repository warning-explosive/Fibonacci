namespace RestBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using TransportApi;

    public class RestBusTransmitter : IBusTransmitter
    {
        private readonly HttpClient _client;
        
        private readonly Uri _uri;

        private readonly int _millisecondsTimeout;
        
        private readonly int _millisecondsPollingDelay;
        
        private readonly ICollection<IRestBusTransmitterFactory> _factories;

        public RestBusTransmitter(Uri uri,
                                  int millisecondsTimeout,
                                  int millisecondsPollingDelay,
                                  IEnumerable<IRestBusTransmitterFactory> factories)
        {
            _uri = uri;
            _client = new HttpClient();
            _millisecondsTimeout = millisecondsTimeout;
            _millisecondsPollingDelay = millisecondsPollingDelay;
            _factories = factories.Reverse().ToList(); // reverse for overrides
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            var timeout = Task.Delay(_millisecondsTimeout);
            var cts = new CancellationTokenSource();
            var work = RequestAsyncInternal<TRequest, TResponse>(request, cts.Token);
            var tasks = new List<Task> { timeout, work };
            
            try
            {
                var first = await Task.WhenAny(tasks);

                if (first == work)
                {
                    return await (Task<TResponse>) first;
                }
                
                cts.Cancel();
                throw new TimeoutException();
            }
            catch (HttpRequestException)
            {
                // do nothing, because it means an external service error
            }
            
            throw new RestBusUnexpectedException();
        }

        private async Task<TResponse> RequestAsyncInternal<TRequest, TResponse>(TRequest request, CancellationToken token)
            where TRequest : class, IRequest<TResponse>
            where TResponse : class, IResponse
        {
            while (!await TryGetAlive())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(_millisecondsPollingDelay, token);
            }

            var postUri = GetPostUri<TRequest, TResponse>(_uri, request);

            using (var response = await _client.PostAsync(postUri, new StringContent(string.Empty), token))
            {
                response.EnsureSuccessStatusCode();
                return ConstructResponse<TRequest, TResponse>(request);
            }
        }

        private async Task<bool> TryGetAlive()
        {
            try
            {
                using (var response = await _client.GetAsync(_uri))
                {
                    var responseMessage = response.EnsureSuccessStatusCode();
                    return responseMessage.StatusCode == HttpStatusCode.OK;
                }
            }
            catch(HttpRequestException)
            {
                return false;
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