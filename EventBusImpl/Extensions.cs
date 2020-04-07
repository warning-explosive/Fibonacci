namespace EventBusImpl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;

    internal static class Extensions
    {
        internal static object DeepCopyBySerialization(this object original)
        {
            original.GetType().GetCustomAttribute<SerializableAttribute>();
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, original);
                stream.Seek(0, SeekOrigin.Begin);
                return binaryFormatter.Deserialize(stream);
            }
        }

        internal static string FlatMessage(this Exception exception)
        {
            var flat = exception is AggregateException aggregateException
                           ? aggregateException.InnerExceptions.SelectMany(Unwrap)
                           : exception.Unwrap();
            
            return string.Join(", ", flat.Select(ex => ex.Message));
        }

        private static IEnumerable<Exception> Unwrap(this Exception exception)
        {
            if (exception is AggregateException aggregate)
            {
                return aggregate.InnerExceptions.SelectMany(Unwrap);
            }
            
            if (exception is TargetInvocationException targetInvocation
             && targetInvocation.InnerException != null)
            {
                return targetInvocation.InnerException.Unwrap();
            }

            exception.InnerException?.Unwrap();

            return new List<Exception> { exception };
        }
    }
}