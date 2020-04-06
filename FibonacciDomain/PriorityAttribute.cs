namespace FibonacciDomain
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PriorityAttribute : Attribute
    {
        public PriorityAttribute(int priorityLevel)
        {
            PriorityLevel = priorityLevel;
        }

        public int PriorityLevel { get; }
    }
}