namespace DevContentEngine.Domain.Services;

public sealed record ActivityScoringInput
{
    public int RelevantCommits { get; }
    public int MergedPullRequests { get; }
    public int ClosedIssuesWithContext { get; }
    public int NewRepositoriesDetected { get; }
    public int TechnologicalDiversity { get; }

    public ActivityScoringInput(
        int relevantCommits,
        int mergedPullRequests,
        int closedIssuesWithContext,
        int newRepositoriesDetected,
        int technologicalDiversity)
    {
        if (relevantCommits < 0)
            throw new ArgumentOutOfRangeException(nameof(relevantCommits), relevantCommits, "RelevantCommits cannot be negative.");

        if (mergedPullRequests < 0)
            throw new ArgumentOutOfRangeException(nameof(mergedPullRequests), mergedPullRequests, "MergedPullRequests cannot be negative.");

        if (closedIssuesWithContext < 0)
            throw new ArgumentOutOfRangeException(nameof(closedIssuesWithContext), closedIssuesWithContext, "ClosedIssuesWithContext cannot be negative.");

        if (newRepositoriesDetected < 0)
            throw new ArgumentOutOfRangeException(nameof(newRepositoriesDetected), newRepositoriesDetected, "NewRepositoriesDetected cannot be negative.");

        if (technologicalDiversity < 0)
            throw new ArgumentOutOfRangeException(nameof(technologicalDiversity), technologicalDiversity, "TechnologicalDiversity cannot be negative.");

        RelevantCommits = relevantCommits;
        MergedPullRequests = mergedPullRequests;
        ClosedIssuesWithContext = closedIssuesWithContext;
        NewRepositoriesDetected = newRepositoriesDetected;
        TechnologicalDiversity = technologicalDiversity;
    }
}
