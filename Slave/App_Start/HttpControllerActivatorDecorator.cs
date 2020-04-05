namespace Slave
{
    using System;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Dispatcher;
    using Controllers;

    public class HttpControllerActivatorDecorator : IHttpControllerActivator
    {
        private readonly IHttpControllerActivator _decoratee;
        private readonly IDependencyResolver _resolver;

        public HttpControllerActivatorDecorator(IHttpControllerActivator decoratee,
                                                IDependencyResolver resolver)
        {
            _decoratee = decoratee;
            _resolver = resolver;
        }
        
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType == typeof(FibonacciController))
            {
                return (IHttpController)_resolver.GetService(controllerType);
            }
            
            return _decoratee.Create(request, controllerDescriptor, controllerType);
        }
    }
}