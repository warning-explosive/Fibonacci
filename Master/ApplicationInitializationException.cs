namespace Master
{
    using System;

    public class ApplicationInitializationException : Exception
    {
        public ApplicationInitializationException(string message)
            : base(message)
        {
        }
    }
}