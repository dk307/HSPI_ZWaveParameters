using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{
    [Serializable]
    public class ShowErrorMessageException : Exception
    {
        public ShowErrorMessageException()
        {
        }

        public ShowErrorMessageException(string message) : base(message)
        {
        }

        public ShowErrorMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ShowErrorMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}