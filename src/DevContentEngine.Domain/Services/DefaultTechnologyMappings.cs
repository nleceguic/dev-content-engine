namespace DevContentEngine.Domain.Services;

public static class DefaultTechnologyMappings
{
    public static IReadOnlyCollection<TechnologyMapping> Mappings { get; } =
    [
        new TechnologyMapping("C#", fileExtensions: [".cs", ".csx"]),
        new TechnologyMapping(
            ".NET",
            fileExtensions: [".csproj", ".sln", ".fsproj", ".vbproj"],
            messageKeywords: ["dotnet", ".net"]),
        new TechnologyMapping("TypeScript", fileExtensions: [".ts", ".tsx"]),
        new TechnologyMapping("JavaScript", fileExtensions: [".js", ".jsx"]),
        new TechnologyMapping("Python", fileExtensions: [".py"]),
        new TechnologyMapping(
            "Docker",
            fileExtensions: [".dockerfile"],
            pathKeywords: ["dockerfile", "docker-compose", "docker/"],
            messageKeywords: ["docker"]),
        new TechnologyMapping(
            "Kubernetes",
            pathKeywords: ["k8s/", "kubernetes/", "helm/"],
            messageKeywords: ["kubernetes", "k8s"]),
        new TechnologyMapping("Kafka", pathKeywords: ["kafka"], messageKeywords: ["kafka"]),
        new TechnologyMapping(
            "PostgreSQL",
            fileExtensions: [".sql"],
            pathKeywords: ["postgres", "postgresql", "pgsql"],
            messageKeywords: ["postgres", "postgresql", "pgsql"]),
        new TechnologyMapping("Redis", pathKeywords: ["redis"], messageKeywords: ["redis"])
    ];
}
