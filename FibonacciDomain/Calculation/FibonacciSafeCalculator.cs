namespace FibonacciDomain.Calculation
{
    public class FibonacciSafeCalculator : IFibonacciCalculator
    {
        public long CalculateNextNumber(long current, long previous)
        {
            return unchecked(current + previous);
        }
    }
}