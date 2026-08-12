namespace DevContentEngine.Infrastructure.GitHub;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string Token { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://api.github.com/graphql";

    public double RateLimitWarningThreshold { get; set; } = 0.1;
}
