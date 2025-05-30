using System;

namespace Users.Infrastructure.Exceptions
{

    public class ValidatorException : Exception
    {
        public ValidatorException(string message) : base(message) { }
    }
}