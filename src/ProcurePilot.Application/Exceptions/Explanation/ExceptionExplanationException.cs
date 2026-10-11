namespace ProcurePilot.Application.Exceptions.Explanation;

public class ExceptionExplanationException : Exception
{
    public ExceptionExplanationException(string message)
        : base(message)
    {
    }

    public ExceptionExplanationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class ExceptionExplanationUnavailableException : ExceptionExplanationException
{
    public ExceptionExplanationUnavailableException(string message)
        : base(message)
    {
    }

    public ExceptionExplanationUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
