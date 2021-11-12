using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{
    [Serializable]
    public class ZWavePlugIsControllerException : Exception
    {
        public ZWavePlugIsControllerException()
        {
        }

        public ZWavePlugIsControllerException(string message) : base(message)
        {
        }

        public ZWavePlugIsControllerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZWavePlugIsControllerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}