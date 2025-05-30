using System;

namespace Users.Infrastructure.Exceptions
{
    public class UserRoleException : Exception
    {
        public UserRoleException(string message) : base(message) { }
    }
}