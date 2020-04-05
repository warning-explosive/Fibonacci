namespace RabbitBasics
{
    using EasyNetQ;

    public class RabbitBusConventionsDecorator : IConventions
    {
        private readonly IConventions _decoratee;

        public RabbitBusConventionsDecorator(IConventions decoratee, string queuePrefix)
        {
            _decoratee = decoratee;
            RpcRoutingKeyNamingConvention = messageType => $"RpcQueue_{messageType.FullName}";
            RpcReturnQueueNamingConvention = () => $"{queuePrefix}RpcReturnQueue";
        }

        public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention
        {
            get => _decoratee.RpcRoutingKeyNamingConvention;
            set => _decoratee.RpcRoutingKeyNamingConvention = value;
        }

        public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention
        {
            get => _decoratee.RpcReturnQueueNamingConvention;
            set => _decoratee.RpcReturnQueueNamingConvention = value;
        }
        
        #region decoratee
        
        public QueueNameConvention QueueNamingConvention
        {
            get => _decoratee.QueueNamingConvention;
            set => _decoratee.QueueNamingConvention = value;
        }
        
        public ExchangeNameConvention ExchangeNamingConvention
        {
            get => _decoratee.ExchangeNamingConvention;
            set => _decoratee.ExchangeNamingConvention = value;
        }
        
        public TopicNameConvention TopicNamingConvention
        {
            get => _decoratee.TopicNamingConvention;
            set => _decoratee.TopicNamingConvention = value;
        }
        
        public ErrorQueueNameConvention ErrorQueueNamingConvention
        {
            get => _decoratee.ErrorQueueNamingConvention;
            set => _decoratee.ErrorQueueNamingConvention = value;
        }
        
        public ErrorExchangeNameConvention ErrorExchangeNamingConvention
        {
            get => _decoratee.ErrorExchangeNamingConvention;
            set => _decoratee.ErrorExchangeNamingConvention = value;
        }
        
        public RpcExchangeNameConvention RpcRequestExchangeNamingConvention
        {
            get => _decoratee.RpcRequestExchangeNamingConvention;
            set => _decoratee.RpcRequestExchangeNamingConvention = value;
        }
        
        public RpcExchangeNameConvention RpcResponseExchangeNamingConvention
        {
            get => _decoratee.RpcResponseExchangeNamingConvention;
            set => _decoratee.RpcResponseExchangeNamingConvention = value;
        }

        public ConsumerTagConvention ConsumerTagConvention
        {
            get => _decoratee.ConsumerTagConvention;
            set => _decoratee.ConsumerTagConvention = value;
        }

        #endregion
    }
}