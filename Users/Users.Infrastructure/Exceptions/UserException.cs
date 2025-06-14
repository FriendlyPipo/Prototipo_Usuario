using System;

namespace Users.Infrastructure.Exceptions
{
    public class UserException : Exception
    {
        public UserException(string message) : base(message) { }
        public UserException(string message, Exception inner) : base(message, inner) { }
    }
}