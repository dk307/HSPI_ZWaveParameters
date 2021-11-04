using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{

    class NotAZWaveDeviceException : Exception
    {
        public NotAZWaveDeviceException()
        {
        }

        public NotAZWaveDeviceException(string message) : base(message)
        {
        }

        public NotAZWaveDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotAZWaveDeviceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
