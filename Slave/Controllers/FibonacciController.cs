namespace Slave.Controllers
{
    using System;
    using System.Web.Http;
    using EventBusApi;
    using FibonacciDomain;
    using FibonacciDomain.DomainEvents;

    public class FibonacciController : ApiController
    {
        private readonly IEventBus _eventBus;

        public FibonacciController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        
        /*
         * Example
         * POST http://localhost:53855/api/fibonacci?calculationId=CDA17C06-6520-4DBE-9DE8-6F231CC75E50&currentNumber=1
         */
        public IHttpActionResult PostCalculation([FromUri] Guid calculationId, [FromUri] long currentNumber)
        {
            try
            {
                var request = new FibonacciRequest
                              {
                                  CalculationId = calculationId,
                                  CurrentNumber = currentNumber
                              };
            
                _eventBus.PlaceEvent(new RequestReceivedEvent<FibonacciRequest>(request));
            }
            catch (Exception)
            {
                return InternalServerError();
            }
            
            return Ok();
        }
    }
}
