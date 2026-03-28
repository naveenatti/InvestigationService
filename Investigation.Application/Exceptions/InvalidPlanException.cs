namespace Investigation.Application.Exceptions
{
    public class InvalidPlanException : Exception
    {
        public InvalidPlanException(string message) : base(message) { }
    }
}