namespace FibonacciDomain
{
    using System;
    using TransportApi;

    [Serializable]
    public class FibonacciRequest : IRequest<AcknowledgementResponse>
    {
        /// <summary>
        /// Calculation process Id
        /// </summary>
        public Guid CalculationId { get; set; }

        /// <summary>
        /// Current number of calculation
        /// </summary>
        public long CurrentNumber { get; set; }
    }
}