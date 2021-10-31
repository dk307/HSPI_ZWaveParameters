using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hspi.Exceptions
{
    internal class ShowErrorMessageException : Exception
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
