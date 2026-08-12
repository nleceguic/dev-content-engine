using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class ActivityScorerTests
{
    [Fact]
    public void Default_options_match_the_blueprint_weights_and_threshold()
    {
        var options = ActivityScoringOptions.Default;

        options.CommitWeight.Should().Be(1.5m);
        options.PullRequestWeight.Should().Be(2.0m);
        options.IssueWeight.Should().Be(1.0m);
        options.NewRepositoryWeight.Should().Be(2.5m);
        options.TechnologicalDiversityWeight.Should().Be(1.0m);
        options.Threshold.Should().Be(5m);
    }

    [Fact]
    public void Score_applies_each_weight_to_its_matching_factor()
    {
        var scorer = new ActivityScorer();

        scorer.Score(new ActivityScoringInput(1, 0, 0, 0, 0)).Score.Should().Be(1.5m);
        scorer.Score(new ActivityScoringInput(0, 1, 0, 0, 0)).Score.Should().Be(2.0m);
        scorer.Score(new ActivityScoringInput(0, 0, 1, 0, 0)).Score.Should().Be(1.0m);
        scorer.Score(new ActivityScoringInput(0, 0, 0, 1, 0)).Score.Should().Be(2.5m);
        scorer.Score(new ActivityScoringInput(0, 0, 0, 0, 1)).Score.Should().Be(1.0m);
    }

    [Fact]
    public void Score_sums_all_weighted_factors_according_to_the_formula()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(
            relevantCommits: 3,
            mergedPullRequests: 2,
            closedIssuesWithContext: 4,
            newRepositoriesDetected: 1,
            technologicalDiversity: 5);

        var result = scorer.Score(input);

        result.Score.Should().Be(20.0m);
        result.ChosenPath.Should().Be(ContentPath.GitHubPath);
    }

    [Fact]
    public void Score_subtracts_the_topic_repetition_penalty()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(4, 0, 0, 0, 0);

        var result = scorer.Score(input, topicRepetitionPenalty: 3.0m);

        result.Score.Should().Be(3.0m);
    }

    [Fact]
    public void ChosenPath_is_GitHubPath_when_activity_is_clearly_strong()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(
            relevantCommits: 5,
            mergedPullRequests: 3,
            closedIssuesWithContext: 2,
            newRepositoriesDetected: 1,
            technologicalDiversity: 3);

        scorer.Score(input).ChosenPath.Should().Be(ContentPath.GitHubPath);
    }

    [Fact]
    public void ChosenPath_is_TrendPath_when_there_is_no_meaningful_GitHub_activity()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(0, 0, 0, 0, 0);

        var result = scorer.Score(input);

        result.Score.Should().Be(0m);
        result.ChosenPath.Should().Be(ContentPath.TrendPath);
    }

    [Fact]
    public void ChosenPath_is_GitHubPath_when_score_lands_exactly_on_the_threshold()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(0, 0, 0, newRepositoriesDetected: 2, technologicalDiversity: 0);

        var result = scorer.Score(input);

        result.Score.Should().Be(5.0m);
        result.ChosenPath.Should().Be(ContentPath.GitHubPath);
    }

    [Fact]
    public void ChosenPath_is_TrendPath_when_score_is_just_below_the_threshold()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(relevantCommits: 3, 0, 0, 0, 0);

        var result = scorer.Score(input);

        result.Score.Should().Be(4.5m);
        result.ChosenPath.Should().Be(ContentPath.TrendPath);
    }

    [Fact]
    public void ChosenPath_is_GitHubPath_when_score_is_just_above_the_threshold()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(relevantCommits: 3, 0, closedIssuesWithContext: 1, 0, 0);

        var result = scorer.Score(input);

        result.Score.Should().Be(5.5m);
        result.ChosenPath.Should().Be(ContentPath.GitHubPath);
    }

    [Fact]
    public void Topic_repetition_penalty_can_flip_the_chosen_path_from_GitHub_to_Trend()
    {
        var scorer = new ActivityScorer();

        var input = new ActivityScoringInput(relevantCommits: 4, 0, 0, 0, 0);

        scorer.Score(input).ChosenPath.Should().Be(ContentPath.GitHubPath);
        scorer.Score(input, topicRepetitionPenalty: 3.0m).ChosenPath.Should().Be(ContentPath.TrendPath);
    }

    [Fact]
    public void Score_uses_injected_weights_instead_of_the_defaults()
    {
        var options = new ActivityScoringOptions(
            commitWeight: 10m,
            pullRequestWeight: 0m,
            issueWeight: 0m,
            newRepositoryWeight: 0m,
            technologicalDiversityWeight: 0m,
            threshold: 15m);

        var scorer = new ActivityScorer(options);

        var input = new ActivityScoringInput(relevantCommits: 1, mergedPullRequests: 100, 0, 0, 0);

        var result = scorer.Score(input);

        result.Score.Should().Be(10m);
        result.ChosenPath.Should().Be(ContentPath.TrendPath);
    }

    [Fact]
    public void Score_uses_the_injected_threshold_instead_of_the_default()
    {
        var options = new ActivityScoringOptions(threshold: 100m);
        var scorer = new ActivityScorer(options);

        var input = new ActivityScoringInput(
            relevantCommits: 5,
            mergedPullRequests: 3,
            closedIssuesWithContext: 2,
            newRepositoriesDetected: 1,
            technologicalDiversity: 3);

        scorer.Score(input).ChosenPath.Should().Be(ContentPath.TrendPath);
    }

    [Fact]
    public void Score_throws_when_input_is_null()
    {
        var scorer = new ActivityScorer();

        var act = () => scorer.Score(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Score_throws_when_penalty_is_negative()
    {
        var scorer = new ActivityScorer();

        var act = () => scorer.Score(new ActivityScoringInput(1, 0, 0, 0, 0), topicRepetitionPenalty: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, -1, 0, 0)]
    [InlineData(0, 0, 0, -1, 0)]
    [InlineData(0, 0, 0, 0, -1)]
    public void ActivityScoringInput_rejects_negative_counts(
        int relevantCommits,
        int mergedPullRequests,
        int closedIssuesWithContext,
        int newRepositoriesDetected,
        int technologicalDiversity)
    {
        var act = () => new ActivityScoringInput(
            relevantCommits,
            mergedPullRequests,
            closedIssuesWithContext,
            newRepositoriesDetected,
            technologicalDiversity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
