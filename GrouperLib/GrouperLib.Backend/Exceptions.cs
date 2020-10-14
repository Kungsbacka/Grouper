using System;

namespace GrouperLib.Backend
{
    public class ChangeRatioException : Exception
    {
        public ChangeRatioException()
        {
        }

        public ChangeRatioException(string message) : base(message)
        {
        }

        public ChangeRatioException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
