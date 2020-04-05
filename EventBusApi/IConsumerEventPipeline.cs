namespace EventBusApi
{
    public interface IConsumerEventPipeline
    {
        void RegisterStep<TDomainEvent>(IPipelineStep<TDomainEvent> eventHandler)
            where TDomainEvent : IDomainEvent;
    }
}
