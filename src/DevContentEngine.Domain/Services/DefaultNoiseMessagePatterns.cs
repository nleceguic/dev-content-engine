namespace DevContentEngine.Domain.Services;

public static class DefaultNoiseMessagePatterns
{
    public static IReadOnlyCollection<string> Patterns { get; } =
    [
        "wip",
        "fix typo",
        "merge branch",
        "formatting",
        "chore: format"
    ];
}
