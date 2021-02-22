using System;
using System.Runtime.Serialization;

namespace QoLFix
{
    public class FailedUpdateException : Exception
    {
        public FailedUpdateException() { }

        public FailedUpdateException(string message) : base(message) { }

        public FailedUpdateException(string message, Exception innerException)
            : base(message, innerException) { }

        protected FailedUpdateException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
