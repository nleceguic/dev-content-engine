namespace DevContentEngine.Domain.Services;

public sealed record ValidationResult
{
    public IReadOnlyCollection<string> FailedRules { get; }
    public IReadOnlyCollection<string> Warnings { get; }

    public bool IsValid => FailedRules.Count == 0;

    public ValidationResult(IReadOnlyCollection<string> failedRules, IReadOnlyCollection<string> warnings)
    {
        FailedRules = failedRules;
        Warnings = warnings;
    }
}
