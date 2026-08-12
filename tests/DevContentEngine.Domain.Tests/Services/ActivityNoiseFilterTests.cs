using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class ActivityNoiseFilterTests
{
    private static readonly Guid RepositoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CommitInfo Commit(
        string message,
        DateTime? timestamp = null,
        int filesChanged = 3,
        int linesChanged = 40,
        Guid? repositoryId = null,
        string? externalId = null)
    {
        return new CommitInfo(
            repositoryId ?? RepositoryId,
            externalId ?? Guid.NewGuid().ToString(),
            message,
            timestamp ?? new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            filesChanged,
            linesChanged);
    }

    [Theory]
    [InlineData("wip")]
    [InlineData("WIP: keep going")]
    [InlineData("fix typo in README")]
    [InlineData("Merge branch 'main' into feature/x")]
    [InlineData("formatting pass")]
    [InlineData("chore: format")]
    public void IsNoise_returns_true_for_messages_matching_default_patterns(string message)
    {
        var filter = new ActivityNoiseFilter();

        filter.IsNoise(Commit(message)).Should().BeTrue();
    }

    [Theory]
    [InlineData("Add retry policy to GitHub activity poller")]
    [InlineData("Implement PostgreSQL repository for GeneratedPost")]
    [InlineData("Fix null reference when repository has no activity")]
    public void IsNoise_returns_false_for_meaningful_commit_messages(string message)
    {
        var filter = new ActivityNoiseFilter();

        filter.IsNoise(Commit(message)).Should().BeFalse();
    }

    [Fact]
    public void IsNoise_returns_true_when_lines_per_file_ratio_suggests_pure_formatting()
    {
        var filter = new ActivityNoiseFilter(formatOnlyLinesPerFileThreshold: 150);

        var commit = Commit("Reformat entire module", filesChanged: 1, linesChanged: 900);

        filter.IsNoise(commit).Should().BeTrue();
    }

    [Fact]
    public void IsNoise_returns_false_when_lines_per_file_ratio_is_within_normal_range()
    {
        var filter = new ActivityNoiseFilter(formatOnlyLinesPerFileThreshold: 150);

        var commit = Commit("Add validation to ContentIdea constructor", filesChanged: 4, linesChanged: 120);

        filter.IsNoise(commit).Should().BeFalse();
    }

    [Fact]
    public void IsNoise_ignores_ratio_check_when_no_files_were_touched()
    {
        var filter = new ActivityNoiseFilter();

        var commit = Commit("Empty commit", filesChanged: 0, linesChanged: 0);

        filter.IsNoise(commit).Should().BeFalse();
    }

    [Fact]
    public void Constructor_accepts_a_custom_pattern_list_instead_of_the_defaults()
    {
        var filter = new ActivityNoiseFilter(noiseMessagePatterns: ["do not ship"]);

        filter.IsNoise(Commit("wip: still broken")).Should().BeFalse();
        filter.IsNoise(Commit("DO NOT SHIP this yet")).Should().BeTrue();
    }

    [Fact]
    public void Constructor_throws_when_the_format_only_threshold_is_not_positive()
    {
        var act = () => new ActivityNoiseFilter(formatOnlyLinesPerFileThreshold: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExcludeNoise_keeps_only_meaningful_commits()
    {
        var filter = new ActivityNoiseFilter();

        var commits = new[]
        {
            Commit("wip"),
            Commit("Add Hangfire recurring job for daily pipeline"),
            Commit("merge branch 'main'"),
            Commit("Implement TechnologyExtractor")
        };

        var result = filter.ExcludeNoise(commits);

        result.Should().HaveCount(2);
        result.Select(commit => commit.Message).Should().BeEquivalentTo(
            "Add Hangfire recurring job for daily pipeline",
            "Implement TechnologyExtractor");
    }

    [Fact]
    public void GroupByDayAndRepository_merges_same_day_same_repository_commits_into_one_signal()
    {
        var filter = new ActivityNoiseFilter();
        var day = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

        var commits = new[]
        {
            Commit("Implement ActivityNoiseFilter", timestamp: day, filesChanged: 2, linesChanged: 30),
            Commit("Add unit tests for ActivityNoiseFilter", timestamp: day.AddHours(3), filesChanged: 1, linesChanged: 20),
            Commit("Wire up TechnologyExtractor defaults", timestamp: day.AddHours(6), filesChanged: 2, linesChanged: 25)
        };

        var groups = filter.GroupByDayAndRepository(commits);

        groups.Should().HaveCount(1);

        var group = groups.Single();
        group.RepositoryId.Should().Be(RepositoryId);
        group.Date.Should().Be(DateOnly.FromDateTime(day));
        group.CommitCount.Should().Be(3);
        group.TotalFilesChanged.Should().Be(5);
        group.TotalLinesChanged.Should().Be(75);
        group.LatestTimestamp.Should().Be(day.AddHours(6));
    }

    [Fact]
    public void GroupByDayAndRepository_keeps_different_days_and_repositories_separate()
    {
        var filter = new ActivityNoiseFilter();
        var otherRepositoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var day1 = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

        var commits = new[]
        {
            Commit("Commit on day 1, repo A", timestamp: day1),
            Commit("Commit on day 2, repo A", timestamp: day2),
            Commit("Commit on day 1, repo B", timestamp: day1, repositoryId: otherRepositoryId)
        };

        var groups = filter.GroupByDayAndRepository(commits);

        groups.Should().HaveCount(3);
    }

    [Fact]
    public void FilterAndGroup_excludes_noise_before_aggregating_the_daily_signal()
    {
        var filter = new ActivityNoiseFilter();
        var day = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);

        var commits = new[]
        {
            Commit("Add repository polling background job", timestamp: day, filesChanged: 2, linesChanged: 60),
            Commit("wip", timestamp: day.AddHours(1), filesChanged: 10, linesChanged: 10),
            Commit("merge branch 'main'", timestamp: day.AddHours(2), filesChanged: 1, linesChanged: 5)
        };

        var groups = filter.FilterAndGroup(commits);

        groups.Should().HaveCount(1);

        var group = groups.Single();
        group.CommitCount.Should().Be(1);
        group.TotalFilesChanged.Should().Be(2);
        group.TotalLinesChanged.Should().Be(60);
    }
}
