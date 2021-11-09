﻿using System;
using System.Runtime.Serialization;

namespace Hspi.Exceptions
{
    internal class ZWavePlugInDataInvalidException : Exception
    {
        public ZWavePlugInDataInvalidException()
        {
        }

        public ZWavePlugInDataInvalidException(string message) : base(message)
        {
        }

        public ZWavePlugInDataInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZWavePlugInDataInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}