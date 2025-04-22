namespace UserMs.Infra.Exceptions
{
    public class UserNotFoundException : System.Exception
    {
        public UserNotFoundException() { }
        public UserNotFoundException(string message) : base(message) { }
        public UserNotFoundException(string message, System.Exception inner) : base(message, inner) { }
    }
}