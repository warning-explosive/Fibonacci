namespace FibonacciDomain.Calculation
{
    using System;

    public interface IFibonacciStorage
    {
        long GetPreviousNumber(Guid calculationId);
        
        void Upsert(Guid calculationId, long nextNumber);
    }
}