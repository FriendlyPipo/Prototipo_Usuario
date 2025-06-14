using System;

namespace Users.Infrastructure.Exceptions
{
    public class KeycloakException : Exception
    {
        public KeycloakException(string message) : base(message) { }
        public KeycloakException(string message, Exception inner) : base(message, inner) { }
    }
}