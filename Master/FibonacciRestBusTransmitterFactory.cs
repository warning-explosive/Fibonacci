namespace Master
{
    using System;
    using System.Text;
    using FibonacciDomain;
    using RestBus;

    public class FibonacciRestBusTransmitterFactory : IRestBusTransmitterGenericFactory<FibonacciRequest, AcknowledgementResponse>
    {
        public AcknowledgementResponse ResponseFactory(FibonacciRequest request)
        {
            return new AcknowledgementResponse();
        }

        public Uri PostUriFactory(Uri baseUri, FibonacciRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(baseUri);
            
            sb.Append("?");
            
            sb.Append(nameof(request.CalculationId).ToLowerInvariant());
            sb.Append("=");
            sb.Append(request.CalculationId);
            
            sb.Append("&");
            
            sb.Append(nameof(request.CurrentNumber).ToLowerInvariant());
            sb.Append("=");
            sb.Append(request.CurrentNumber);
            
            return new Uri(sb.ToString());
        }
    }
}