namespace FibonacciDomain.Calculation
{
    public interface IFibonacciCalculator
    {
        long CalculateNextNumber(long current, long previous);
    }
}