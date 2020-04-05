namespace Slave
{
    using System.Web.Http;
    using EventBusApi;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(Register);
        }
        
        public static void Register(HttpConfiguration config)
        {
            var wrappedResolver = new PureDIDependencyResolverDecorator(config.DependencyResolver);
            config.DependencyResolver = wrappedResolver;
            
            /*
             * Invoke functionality
             *     - start event sourcing processing
             *     - run asynchronous calculations -> FibonacciController
             */
            var eventBus = (IProducerEventPipeline)config.DependencyResolver.GetService(typeof(IProducerEventPipeline));
            eventBus.LockAndRunEventLoop();
            
            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute("DefaultApi",
                                       "api/{controller}/{id}",
                                       new { id = RouteParameter.Optional });
        }
    }
}
