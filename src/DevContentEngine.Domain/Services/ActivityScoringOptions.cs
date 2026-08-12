namespace DevContentEngine.Domain.Services;

public sealed class ActivityScoringOptions
{
    public const decimal DefaultCommitWeight = 1.5m;
    public const decimal DefaultPullRequestWeight = 2.0m;
    public const decimal DefaultIssueWeight = 1.0m;
    public const decimal DefaultNewRepositoryWeight = 2.5m;
    public const decimal DefaultTechnologicalDiversityWeight = 1.0m;
    public const decimal DefaultThreshold = 5m;

    public decimal CommitWeight { get; }
    public decimal PullRequestWeight { get; }
    public decimal IssueWeight { get; }
    public decimal NewRepositoryWeight { get; }
    public decimal TechnologicalDiversityWeight { get; }
    public decimal Threshold { get; }

    public ActivityScoringOptions(
        decimal commitWeight = DefaultCommitWeight,
        decimal pullRequestWeight = DefaultPullRequestWeight,
        decimal issueWeight = DefaultIssueWeight,
        decimal newRepositoryWeight = DefaultNewRepositoryWeight,
        decimal technologicalDiversityWeight = DefaultTechnologicalDiversityWeight,
        decimal threshold = DefaultThreshold)
    {
        CommitWeight = commitWeight;
        PullRequestWeight = pullRequestWeight;
        IssueWeight = issueWeight;
        NewRepositoryWeight = newRepositoryWeight;
        TechnologicalDiversityWeight = technologicalDiversityWeight;
        Threshold = threshold;
    }

    public static ActivityScoringOptions Default { get; } = new();
}
