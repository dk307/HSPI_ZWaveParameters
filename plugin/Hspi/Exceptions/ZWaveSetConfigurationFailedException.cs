using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{
    [Serializable]
    public class ZWaveSetConfigurationFailedException : Exception
    {
        public ZWaveSetConfigurationFailedException()
        {
        }

        public ZWaveSetConfigurationFailedException(string message) : base(message)
        {
        }

        public ZWaveSetConfigurationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZWaveSetConfigurationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}