namespace FibonacciDomain.Calculation
{
    using System;
    using System.Collections.Concurrent;

    public class FibonacciConcurrentStorage : IFibonacciStorage
    {
        private static readonly ConcurrentDictionary<Guid, long> Calculations = new ConcurrentDictionary<Guid, long>();
        
        private readonly long _defaultValue;

        public FibonacciConcurrentStorage(long defaultValue)
        {
            _defaultValue = defaultValue;
        }
        
        public long GetPreviousNumber(Guid calculationId)
        {
            return Calculations.GetOrAdd(calculationId, _defaultValue);
        }

        public void Upsert(Guid calculationId, long nextNumber)
        {
            Calculations.AddOrUpdate(calculationId, id => nextNumber, (id, old) => nextNumber);
        }
    }
}