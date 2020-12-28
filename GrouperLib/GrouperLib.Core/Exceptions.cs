using System;

namespace GrouperLib.Core
{
    public sealed class InvalidGrouperDocumentException : Exception
    {
        public InvalidGrouperDocumentException()
        {
        }

        public InvalidGrouperDocumentException(string message) : base(message)
        {
        }

        public InvalidGrouperDocumentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
