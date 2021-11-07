using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{

    internal class ZWaveGetConfigurationFailedException : Exception
    {
        public ZWaveGetConfigurationFailedException()
        {
        }

        public ZWaveGetConfigurationFailedException(string message) : base(message)
        {
        }

        public ZWaveGetConfigurationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZWaveGetConfigurationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}