namespace DevContentEngine.Infrastructure.GitHub;

public sealed class GitHubIntegrationException : Exception
{
    public GitHubIntegrationException(string message)
        : base(message)
    {
    }

    public GitHubIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
