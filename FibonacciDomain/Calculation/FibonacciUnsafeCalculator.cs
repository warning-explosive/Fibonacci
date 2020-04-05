namespace FibonacciDomain.Calculation
{
    public class FibonacciUnsafeCalculator : IFibonacciCalculator
    {
        public long CalculateNextNumber(long current, long previous)
        {
            return checked(current + previous);
        }
    }
}
