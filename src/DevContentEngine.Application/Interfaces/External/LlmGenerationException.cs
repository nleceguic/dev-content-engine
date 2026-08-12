namespace DevContentEngine.Application.Interfaces.External;

public sealed class LlmGenerationException : Exception
{
    public LlmGenerationException(string message)
        : base(message)
    {
    }

    public LlmGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
