using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{
    [Serializable]
    public class ZWavePluginNotRunningException : Exception
    {
        public ZWavePluginNotRunningException()
        {
        }

        public ZWavePluginNotRunningException(string message) : base(message)
        {
        }

        public ZWavePluginNotRunningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZWavePluginNotRunningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
