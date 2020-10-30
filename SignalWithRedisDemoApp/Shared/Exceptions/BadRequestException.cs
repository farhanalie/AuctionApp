using System;

namespace SignalWithRedisDemoApp.Shared.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message = "Invalid request")
            : base(message) { }
    }
}