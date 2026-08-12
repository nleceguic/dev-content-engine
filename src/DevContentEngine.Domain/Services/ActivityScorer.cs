using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Domain.Services;

public sealed class ActivityScorer
{
    private readonly ActivityScoringOptions _options;

    public ActivityScorer(ActivityScoringOptions? options = null)
    {
        _options = options ?? ActivityScoringOptions.Default;
    }

    public ActivityScoreResult Score(ActivityScoringInput input, decimal topicRepetitionPenalty = 0m)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (topicRepetitionPenalty < 0)
            throw new ArgumentOutOfRangeException(
                nameof(topicRepetitionPenalty),
                topicRepetitionPenalty,
                "Penalty must be expressed as a non-negative magnitude to subtract from the score.");

        var score =
            (_options.CommitWeight * input.RelevantCommits) +
            (_options.PullRequestWeight * input.MergedPullRequests) +
            (_options.IssueWeight * input.ClosedIssuesWithContext) +
            (_options.NewRepositoryWeight * input.NewRepositoriesDetected) +
            (_options.TechnologicalDiversityWeight * input.TechnologicalDiversity) -
            topicRepetitionPenalty;

        var chosenPath = score >= _options.Threshold ? ContentPath.GitHubPath : ContentPath.TrendPath;

        return new ActivityScoreResult(score, chosenPath);
    }
}
