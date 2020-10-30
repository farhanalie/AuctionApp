using System;

namespace AuctionApp.Shared.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message = "Invalid request")
            : base(message) { }
    }
}