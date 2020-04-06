namespace FibonacciDomain.Steps
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Calculation;
    using DomainEvents;
    using EventBusApi;

    public class CalculationPipelineStep : IPipelineStep<RequestReceivedEvent<FibonacciRequest>>
    {
        private readonly IFibonacciCalculator _calculator;
        
        private readonly IFibonacciStorage _storage;

        public CalculationPipelineStep(IFibonacciCalculator calculator,
                                       IFibonacciStorage storage)
        {
            _calculator = calculator;
            _storage = storage;
        }
        
        public Task<IReadOnlyCollection<IDomainEvent>> HandleEvent(RequestReceivedEvent<FibonacciRequest> domainEvent)
        {
            var nextNumber = _calculator.CalculateNextNumber(domainEvent.Request.CurrentNumber, _storage.GetPreviousNumber(domainEvent.Request.CalculationId));

            IReadOnlyCollection<IDomainEvent> events = new List<IDomainEvent> { new CalculatedEvent(domainEvent.Request.CalculationId, nextNumber) };

            return Task.FromResult(events);
        }
    }
}