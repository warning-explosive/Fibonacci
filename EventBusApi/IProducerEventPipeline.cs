namespace EventBusApi
{
    public interface IProducerEventPipeline
    {
        void LockAndRunEventLoop();

        void StopAndRelease();
    }
}