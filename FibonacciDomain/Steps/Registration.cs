namespace FibonacciDomain.Steps
{
    using Calculation;
    using EventBusApi;
    using TransportApi;

    public static class Registration
    {
        public static void RegisterPipeline(IConsumerEventPipeline eventPipeline,
                                            IBusTransmitter busTransmitter,
                                            long storageDefault)
        {
            var fibonacciCalculator = new FibonacciUnsafeCalculator();
            var fibonacciStorage = new FibonacciConcurrentStorage(storageDefault);
            
            var wrappedCalculation = new CalculationPipelineStepDecorator(new CalculationPipelineStep(fibonacciCalculator, fibonacciStorage));
            eventPipeline.RegisterStep(wrappedCalculation);
            
            var wrappedStore = new StoreCalculationPipelineStepDecorator(new StoreCalculationPipelineStep(fibonacciStorage));            
            eventPipeline.RegisterStep(wrappedStore);
            
            var reply = new ReplyPipelineStepDecorator(new ReplyPipelineStep(busTransmitter));
            eventPipeline.RegisterStep(reply);
        }
    }
}