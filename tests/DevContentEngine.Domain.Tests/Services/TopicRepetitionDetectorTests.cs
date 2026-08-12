using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class TopicRepetitionDetectorTests
{
    private static readonly DateTime ReferenceDate = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Default_options_match_the_blueprint_threshold_penalty_and_window()
    {
        var options = TopicRepetitionOptions.Default;

        options.OverlapThreshold.Should().Be(0.6);
        options.Penalty.Should().Be(3.0m);
        options.LookbackWindow.Should().Be(TimeSpan.FromDays(14));
    }

    [Fact]
    public void Detect_flags_similarity_when_keywords_fully_overlap()
    {
        var detector = new TopicRepetitionDetector();
        var recentPostId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(recentPostId, ["kafka", "docker", "postgresql"], ReferenceDate.AddDays(-2))
        };

        var result = detector.Detect(["kafka", "docker", "postgresql"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeTrue();
        result.Penalty.Should().Be(3.0m);
        result.MostSimilarPostId.Should().Be(recentPostId);
        result.HighestOverlapRatio.Should().Be(1.0);
    }

    [Fact]
    public void Detect_does_not_flag_completely_disjoint_topics()
    {
        var detector = new TopicRepetitionDetector();

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["python", "flask"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["kafka", "docker"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeFalse();
        result.Penalty.Should().Be(0m);
        result.MostSimilarPostId.Should().BeNull();
    }

    [Fact]
    public void Detect_flags_similarity_when_overlap_lands_exactly_on_the_threshold()
    {
        var detector = new TopicRepetitionDetector();
        var recentPostId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(recentPostId, ["a", "b", "c", "d", "e"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["a", "b", "c"], recentPosts, ReferenceDate);

        result.HighestOverlapRatio.Should().Be(0.6);
        result.SimilarToRecentPost.Should().BeTrue();
        result.Penalty.Should().Be(3.0m);
    }

    [Fact]
    public void Detect_does_not_flag_similarity_when_overlap_is_just_below_the_threshold()
    {
        var detector = new TopicRepetitionDetector();

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["a", "b", "c", "d"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["a", "b"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeFalse();
        result.Penalty.Should().Be(0m);
        result.HighestOverlapRatio.Should().BeNull();
    }

    [Fact]
    public void Detect_flags_similarity_when_overlap_is_just_above_the_threshold()
    {
        var detector = new TopicRepetitionDetector();
        var recentPostId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(recentPostId, ["a", "b", "c"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["a", "b"], recentPosts, ReferenceDate);

        result.HighestOverlapRatio.Should().BeApproximately(0.6667, 0.0001);
        result.SimilarToRecentPost.Should().BeTrue();
        result.MostSimilarPostId.Should().Be(recentPostId);
    }

    [Fact]
    public void Detect_picks_the_recent_post_with_the_highest_overlap_among_several()
    {
        var detector = new TopicRepetitionDetector();

        var lowOverlapPostId = Guid.NewGuid();
        var highOverlapPostId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(lowOverlapPostId, ["kafka", "unrelated"], ReferenceDate.AddDays(-1)),
            new RecentPostTopics(highOverlapPostId, ["kafka", "docker", "postgresql"], ReferenceDate.AddDays(-3))
        };

        var result = detector.Detect(["kafka", "docker", "postgresql"], recentPosts, ReferenceDate);

        result.MostSimilarPostId.Should().Be(highOverlapPostId);
        result.HighestOverlapRatio.Should().Be(1.0);
    }

    [Fact]
    public void Detect_ignores_posts_published_outside_the_lookback_window()
    {
        var detector = new TopicRepetitionDetector();

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["kafka", "docker", "postgresql"], ReferenceDate.AddDays(-20))
        };

        var result = detector.Detect(["kafka", "docker", "postgresql"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeFalse();
    }

    [Fact]
    public void Detect_includes_a_post_published_exactly_at_the_edge_of_the_lookback_window()
    {
        var detector = new TopicRepetitionDetector();
        var postId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(postId, ["kafka", "docker", "postgresql"], ReferenceDate - TimeSpan.FromDays(14))
        };

        var result = detector.Detect(["kafka", "docker", "postgresql"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeTrue();
        result.MostSimilarPostId.Should().Be(postId);
    }

    [Fact]
    public void Detect_is_case_insensitive_when_comparing_keywords()
    {
        var detector = new TopicRepetitionDetector();
        var postId = Guid.NewGuid();

        var recentPosts = new[]
        {
            new RecentPostTopics(postId, ["Kafka", "Docker", "PostgreSQL"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["kafka", "docker", "postgresql"], recentPosts, ReferenceDate);

        result.HighestOverlapRatio.Should().Be(1.0);
        result.SimilarToRecentPost.Should().BeTrue();
    }

    [Fact]
    public void Detect_returns_no_similarity_when_there_are_no_recent_posts()
    {
        var detector = new TopicRepetitionDetector();

        var result = detector.Detect(["kafka", "docker"], [], ReferenceDate);

        result.Should().Be(TopicRepetitionResult.None);
    }

    [Fact]
    public void Detect_returns_no_similarity_when_candidate_keywords_are_empty()
    {
        var detector = new TopicRepetitionDetector();

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["kafka", "docker"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect([], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeFalse();
    }

    [Fact]
    public void Detect_uses_injected_threshold_and_penalty_instead_of_the_defaults()
    {
        var options = new TopicRepetitionOptions(overlapThreshold: 0.9, penalty: 1.5m);
        var detector = new TopicRepetitionDetector(options);

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["a", "b", "c"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["a", "b"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeFalse();
    }

    [Fact]
    public void Detect_applies_the_injected_penalty_value_when_similarity_is_found()
    {
        var options = new TopicRepetitionOptions(overlapThreshold: 0.5, penalty: 1.5m);
        var detector = new TopicRepetitionDetector(options);

        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["a", "b"], ReferenceDate.AddDays(-1))
        };

        var result = detector.Detect(["a", "b"], recentPosts, ReferenceDate);

        result.SimilarToRecentPost.Should().BeTrue();
        result.Penalty.Should().Be(1.5m);
    }

    [Fact]
    public void Detect_throws_when_candidate_keywords_is_null()
    {
        var detector = new TopicRepetitionDetector();

        var act = () => detector.Detect(null!, [], ReferenceDate);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Detect_throws_when_recent_posts_is_null()
    {
        var detector = new TopicRepetitionDetector();

        var act = () => detector.Detect(["kafka"], null!, ReferenceDate);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.1)]
    [InlineData(-0.5)]
    public void TopicRepetitionOptions_rejects_an_overlap_threshold_outside_zero_to_one(double threshold)
    {
        var act = () => new TopicRepetitionOptions(overlapThreshold: threshold);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TopicRepetitionOptions_rejects_a_negative_penalty()
    {
        var act = () => new TopicRepetitionOptions(penalty: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TopicRepetitionOptions_rejects_a_non_positive_lookback_window()
    {
        var act = () => new TopicRepetitionOptions(lookbackWindow: TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
