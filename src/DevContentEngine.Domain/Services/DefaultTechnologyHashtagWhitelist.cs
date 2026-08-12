namespace DevContentEngine.Domain.Services;

public static class DefaultTechnologyHashtagWhitelist
{
    public static IReadOnlyCollection<string> Hashtags { get; } =
    [
        "dotnet", "csharp", "aspnetcore", "docker", "kubernetes", "postgresql", "postgres",
        "kafka", "redis", "typescript", "javascript", "python", "github", "githubactions",
        "cicd", "devops", "cloud", "aws", "azure", "gcp", "microservices", "api", "backend",
        "opensource", "softwareengineering", "programming", "webdev", "cleanarchitecture",
        "sql", "nosql", "linux", "grpc", "graphql", "rest",
        "llm", "ai", "anthropic", "claude", "ddd", "cqrs", "mediatr", "efcore",
        "entityframework", "hangfire", "automation", "buildinpublic", "opentowork"
    ];
}
