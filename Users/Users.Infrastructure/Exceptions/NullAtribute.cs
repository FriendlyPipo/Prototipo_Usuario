namespace Users.Infrastructure.Exceptions
{
    public class NullAtributeException : System.Exception
    {
        public NullAtributeException() { }
        public NullAtributeException(string message) : base(message) { }
        public NullAtributeException(string message, System.Exception inner) : base(message, inner) { }
    }
}