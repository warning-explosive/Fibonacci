namespace Master
{
    using System;
    using System.Collections.Generic;
    using EasyNetQ;
    using EventBusApi;
    using EventBusImpl;
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
                                     out var queuePrefix);

            /*
             * Composition root - build object graph
             */
            var eventBus = new EventBus();
            var producerEventPipeline = ComposeObjectGraph(eventBus,
                                                           rabbitConnectionConfig,
                                                           storageDefault,
                                                           slaveApiUri,
                                                           queuePrefix);
            
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
                 *     - TODO: stop workers
                 *     - TODO: remove RabbitMQ queues
                 *     - clean up disposables
                 */
                producerEventPipeline.StopAndRelease();
                
                foreach (var disposable in Disposables)
                {
                    disposable?.Dispose();
                }
            }
        }

        private static void RunCalculations(IEventBus eventBus, uint asyncCalculationsCount, long storageDefault)
        {
            for (var i = 0; i < asyncCalculationsCount; ++i)
            {
                eventBus.PlaceEvent(new CalculatedEvent(Guid.NewGuid(), storageDefault));
            }
        }

        private static void GetExternalConfiguration(string[] args,
                                                     out uint asyncCalculationsCount,
                                                     out RabbitConnectionConfig rabbitConnectionConfig,
                                                     out long storageDefault,
                                                     out Uri slaveApiBaseUri,
                                                     out string queuePrefix)
        {
            asyncCalculationsCount = ExtractConsoleArgument(args);
            
            rabbitConnectionConfig = new RabbitConnectionConfig
                                     {
                                         Host = "localhost",
                                         VirtualHost = "/",
                                         Username = "guest",
                                         Password = "guest",
                                         Product = nameof(Master),
                                         SecondsTimeout = 10,
                                     };

            storageDefault = 0;

            slaveApiBaseUri = new Uri("http://localhost:53855/api/fibonacci");

            queuePrefix = nameof(Master);
        }

        private static uint ExtractConsoleArgument(string[] args)
        {
            try
            {
                return Convert.ToUInt32(string.Join(string.Empty, args));
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
                                                                 string queuePrefix)
        {
            var eventPipeline = new EventPipeline(eventBus);

            var busTransmitter = new RestBusTransmitter(slaveApiBaseUri,
                                                        rabbitConnectionConfig.SecondsTimeout * 1000,
                                                        500,
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
