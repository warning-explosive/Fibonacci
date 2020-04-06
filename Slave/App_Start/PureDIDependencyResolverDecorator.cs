namespace Slave
{
    using System;
    using System.Collections.Generic;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Dispatcher;
    using Controllers;
    using EasyNetQ;
    using EventBusApi;
    using EventBusImpl;
    using FibonacciDomain.Steps;
    using RabbitBasics;
    using EventBus = EventBusImpl.EventBus;
    using IEventBus = EventBusApi.IEventBus;

    public class PureDIDependencyResolverDecorator : IDependencyResolver
    {
        private readonly IDependencyResolver _decoratee;

        private readonly IHttpControllerActivator _activator;
        
        private readonly IEventBus _eventBus;
        
        private readonly IProducerEventPipeline _eventPipeline;

        private readonly ICollection<IDisposable> _disposables = new List<IDisposable>();

        public PureDIDependencyResolverDecorator(IDependencyResolver decoratee)
        {
            _decoratee = decoratee;
            
            /*
             * Configuration section
             * In read world must be configured by CD server and read by app as configuration files or as cli-arguments
             */
            GetExternalConfiguration(out var rabbitConnectionConfig,
                                     out var storageDefault,
                                     out var queuePrefix,
                                     out var concurrencyLevel,
                                     out var retryLimit);
            
            /*
             * Composition root - build object graph
             */
            _activator = new HttpControllerActivatorDecorator(new DefaultHttpControllerActivator(), this);
            _eventBus = new EventBus();
            _eventPipeline = ComposeObjectGraph(_eventBus,
                                                rabbitConnectionConfig,
                                                storageDefault,
                                                queuePrefix,
                                                concurrencyLevel,
                                                retryLimit);
        }

        public void Dispose()
        {
            /*
             * Release graph
             *     - TODO: stop workers
             *     - TODO: remove RabbitMQ queues
             *     - clean up disposables
             */
            _eventPipeline.StopAndRelease();
            
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            
            _decoratee.Dispose();
        }

        public object GetService(Type serviceType)
        {
            /*
             * Singleton lifestyle components
             */
            if (serviceType == typeof(IHttpControllerActivator))
            {
                return _activator;
            }

            if (serviceType == typeof(IProducerEventPipeline))
            {
                return _eventPipeline;
            }
            
            /*
             * Transient lifestyle components
             */
            if (serviceType == typeof(FibonacciController))
            {
                return new FibonacciController(_eventBus);
            }
            
            /*
             * Other system components
             */
            return _decoratee.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _decoratee.GetServices(serviceType);
        }

        public IDependencyScope BeginScope()
        {
            return _decoratee.BeginScope();
        }
        
        private void GetExternalConfiguration(out RabbitConnectionConfig rabbitConnectionConfig,
                                              out long storageDefault,
                                              out string queuePrefix,
                                              out int concurrencyLevel,
                                              out int retryLimit)
        {
            rabbitConnectionConfig = new RabbitConnectionConfig
                                     {
                                         Host = "localhost",
                                         VirtualHost = "/",
                                         Username = "guest",
                                         Password = "guest",
                                         Product = nameof(Slave),
                                         SecondsTimeout = 3,
                                     };

            storageDefault = 1;

            queuePrefix = nameof(Slave);

            concurrencyLevel = 10;

            retryLimit = 5;
        }
        
        private IProducerEventPipeline ComposeObjectGraph(IEventBus eventBus,
                                                          RabbitConnectionConfig rabbitConnectionConfig,
                                                          long storageDefault,
                                                          string queuePrefix,
                                                          int concurrencyLevel,
                                                          int retryLimit)
        {
            var eventPipeline = new EventPipeline(eventBus, concurrencyLevel, retryLimit);
            
            var conventions = new RabbitBusConventionsDecorator(new Conventions(new DefaultTypeNameSerializer()), queuePrefix);
            var busTransmitter = new RabbitBusTransmitter(rabbitConnectionConfig, conventions);
            Registration.RegisterPipeline(eventPipeline, busTransmitter, storageDefault);
            
            _disposables.Add(busTransmitter);
            
            return eventPipeline;
        }
    }
}