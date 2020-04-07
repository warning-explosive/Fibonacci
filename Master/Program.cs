namespace Master
{
    using System;
    using System.Collections.Generic;
    using EasyNetQ;
    using EventBusApi;
    using EventBusImpl;
    using FibonacciDomain;
    using FibonacciDomain.DomainEvents;
    using FibonacciDomain.Steps;
    using RabbitBasics;
    using RestBus;
    using EventBus = EventBusImpl.EventBus;
    using IEventBus = EventBusApi.IEventBus;

    public static class Program
    {
        private static readonly ICollection<IDisposable> Disposables = new List<IDisposable>();
        
        public static void Main(string[] args)
        {
            /*
             * Configuration section
             * In read world must be configured by CD server and read by app as configuration files or as cli-arguments
             */
            GetExternalConfiguration(args,
                                     out var asyncCalculationsCount,
                                     out var rabbitConnectionConfig,
                                     out var storageDefault,
                                     out var slaveApiUri,
                                     out var restSecondsTimeout,
                                     out var queuePrefix,
                                     out var concurrencyLevel,
                                     out var retryLimit);

            /*
             * Composition root - build object graph
             */
            var eventBus = new HybridEventBus(new DummyEventBusWithPriority(), new EventBus());
            var producerEventPipeline = ComposeObjectGraph(eventBus,
                                                           rabbitConnectionConfig,
                                                           storageDefault,
                                                           slaveApiUri,
                                                           restSecondsTimeout,
                                                           queuePrefix,
                                                           concurrencyLevel,
                                                           retryLimit);
            
            /*
             * Invoke functionality
             *     - start event sourcing processing
             *     - run asynchronous calculations
             */
            try
            {
                producerEventPipeline.LockAndRunEventLoop();
                RunCalculations(eventBus, asyncCalculationsCount, storageDefault);
                Console.ReadKey();
            }
            finally
            {
                /*
                 * Release graph
                 *     - stop workers
                 *     - remove RabbitMQ queues (RabbitBusReceiver have durable queue)
                 *     - clean up disposables
                 */
                producerEventPipeline.StopAndRelease();
                
                foreach (var disposable in Disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        private static void RunCalculations(IEventBus eventBus, int asyncCalculationsCount, long storageDefault)
        {
            for (var i = 0; i < asyncCalculationsCount; ++i)
            {
                eventBus.PlaceEvent(new CalculatedEvent(Guid.NewGuid(), storageDefault));
            }
        }

        private static void GetExternalConfiguration(string[] args,
                                                     out int asyncCalculationsCount,
                                                     out RabbitConnectionConfig rabbitConnectionConfig,
                                                     out long storageDefault,
                                                     out Uri slaveApiBaseUri,
                                                     out int restSecondsTimeout,
                                                     out string queuePrefix,
                                                     out int concurrencyLevel,
                                                     out int retryLimit)
        {
            asyncCalculationsCount = ExtractConsoleArgument(args);
            
            rabbitConnectionConfig = new RabbitConnectionConfig
                                     {
                                         Host = "localhost",
                                         VirtualHost = "/",
                                         Username = "guest",
                                         Password = "guest",
                                         Product = nameof(Master),
                                         SecondsTimeout = 3,
                                     };

            storageDefault = 0;

            slaveApiBaseUri = new Uri("http://localhost:53855/api/fibonacci");
            
            restSecondsTimeout = 3;
            
            queuePrefix = nameof(Master);

            concurrencyLevel = 10;

            retryLimit = 5;
        }

        private static int ExtractConsoleArgument(string[] args)
        {
            try
            {
                return Convert.ToInt32(string.Join(string.Empty, args));
            }
            catch (Exception)
            {
                throw new ApplicationInitializationException("Wrong cli args!");
            }
        }

        private static IProducerEventPipeline ComposeObjectGraph(IEventBus eventBus,
                                                                 RabbitConnectionConfig rabbitConnectionConfig,
                                                                 long storageDefault,
                                                                 Uri slaveApiBaseUri,
                                                                 int restSecondsTimeout,
                                                                 string queuePrefix,
                                                                 int concurrencyLevel,
                                                                 int retryLimit)
        {
            var eventPipeline = new EventPipeline(eventBus, concurrencyLevel, retryLimit);

            var busTransmitter = new RestBusTransmitter(slaveApiBaseUri,
                                                        restSecondsTimeout * 1000,
                                                        new List<IRestBusTransmitterFactory>
                                                        {
                                                            new FibonacciRestBusTransmitterFactory() // strategies
                                                        });
            Registration.RegisterPipeline(eventPipeline, busTransmitter, storageDefault);

            var conventions = new RabbitBusConventionsDecorator(new Conventions(new DefaultTypeNameSerializer()), queuePrefix);
            var busReceiver = new RabbitBusReceiver(rabbitConnectionConfig, conventions);
            var handler = new FibonacciRabbitRequestHandler(eventBus);
            busReceiver.Register(handler);

            Disposables.Add(busTransmitter);
            Disposables.Add(busReceiver);
            
            return eventPipeline;
        }
    }
}
