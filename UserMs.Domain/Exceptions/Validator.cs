namespace UserMs.Domain.Exceptions
{
    public class ValidatorException : System.Exception
    {
        public ValidatorException() { }
        public ValidatorException(string message) : base(message) { }
        public ValidatorException(string message, System.Exception inner) : base(message, inner) { }

    }
}