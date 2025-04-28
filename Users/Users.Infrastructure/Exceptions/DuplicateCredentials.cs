namespace Users.Infrastructure.Exceptions
{
    public class DuplicateCredentialsException : System.Exception
    {
        public DuplicateCredentialsException() { }
        public DuplicateCredentialsException(string message) : base(message) { }
        public DuplicateCredentialsException(string message, System.Exception inner) : base(message, inner) { }
    }
}