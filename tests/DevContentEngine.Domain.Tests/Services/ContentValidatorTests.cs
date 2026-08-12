using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class ContentValidatorTests
{
    private static readonly DateTime ReferenceDate = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private static DraftContent Draft(
        int totalLength = 1000,
        IReadOnlyCollection<string>? hashtags = null,
        IReadOnlyCollection<string>? sources = null)
    {
        const string hook = "Conecté el pipeline diario con GitHub y Postgres esta semana.";
        const string conclusion = "Fue un buen ejercicio de diseño en capas y trazabilidad.";
        const string cta = "¿Qué opinas del enfoque?";
        var fixedLength = hook.Length + conclusion.Length + cta.Length;
        var body = new string('a', Math.Max(1, totalLength - fixedLength));

        return new DraftContent(
            hook,
            body,
            conclusion,
            cta,
            hashtags ?? ["#dotnet", "#postgresql"],
            sources ?? ["https://github.com/owner/repo/commit/abc123"]);
    }

    [Fact]
    public void Validate_passes_a_well_formed_GitHub_draft()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeTrue();
        result.FailedRules.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(699)]
    [InlineData(1601)]
    public void Validate_fails_when_total_length_is_outside_the_700_to_1600_window(int totalLength)
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(totalLength: totalLength), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("expected between 700 and 1600"));
    }

    [Theory]
    [InlineData(700)]
    [InlineData(1600)]
    public void Validate_passes_at_the_exact_length_boundaries(int totalLength)
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(totalLength: totalLength), ContentOrigin.GitHub, [], ReferenceDate);

        result.FailedRules.Should().NotContain(rule => rule.Contains("expected between"));
    }

    [Fact]
    public void Validate_fails_when_there_are_more_than_five_hashtags()
    {
        var validator = new ContentValidator();
        var hashtags = new[] { "#dotnet", "#csharp", "#docker", "#postgresql", "#kubernetes", "#kafka" };

        var result = validator.Validate(Draft(hashtags: hashtags), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("maximum allowed is 5"));
    }

    [Fact]
    public void Validate_passes_with_exactly_five_whitelisted_hashtags()
    {
        var validator = new ContentValidator();
        var hashtags = new[] { "#dotnet", "#csharp", "#docker", "#postgresql", "#kubernetes" };

        var result = validator.Validate(Draft(hashtags: hashtags), ContentOrigin.GitHub, [], ReferenceDate);

        result.FailedRules.Should().NotContain(rule => rule.Contains("maximum allowed") || rule.Contains("whitelist"));
    }

    [Fact]
    public void Validate_fails_when_a_hashtag_is_not_in_the_technology_whitelist()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(hashtags: ["#dotnet", "#motivation"]), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("whitelist") && rule.Contains("#motivation"));
    }

    [Fact]
    public void Validate_matches_whitelist_entries_case_insensitively_and_ignoring_the_hash_prefix()
    {
        var validator = new ContentValidator(technologyWhitelist: ["dotnet"]);

        var result = validator.Validate(Draft(hashtags: ["#DotNet"]), ContentOrigin.GitHub, [], ReferenceDate);

        result.FailedRules.Should().NotContain(rule => rule.Contains("whitelist"));
    }

    [Theory]
    [InlineData("Este post habla sobre la importancia de medir bien.")]
    [InlineData("NO VAS A CREER lo que encontré en el código.")]
    [InlineData("Esto cambiará tu carrera para siempre.")]
    public void Validate_fails_when_the_content_contains_a_banned_clickbait_phrase(string bodyText)
    {
        var validator = new ContentValidator();
        var draft = Draft() with { Body = bodyText + new string('a', 900) };

        var result = validator.Validate(draft, ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("forbidden/clickbait phrase"));
    }

    [Fact]
    public void Validate_uses_injected_banned_phrases_instead_of_the_defaults()
    {
        var validator = new ContentValidator(bannedPhrases: ["conecté el pipeline"]);

        var result = validator.Validate(Draft(), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("conecté el pipeline"));
    }

    [Fact]
    public void Validate_fails_when_there_are_no_sources_regardless_of_origin()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(sources: []), ContentOrigin.GitHub, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("traceable to at least one real source"));
    }

    [Fact]
    public void Validate_allows_non_url_sources_for_GitHub_origin()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(sources: ["sha-abc123"]), ContentOrigin.GitHub, [], ReferenceDate);

        result.FailedRules.Should().NotContain(rule => rule.Contains("valid URL"));
    }

    [Fact]
    public void Validate_fails_for_Trend_origin_when_no_source_is_a_valid_URL()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(sources: ["not-a-url"]), ContentOrigin.Trend, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("trend-originated post must include at least one source with a valid URL"));
    }

    [Fact]
    public void Validate_passes_the_source_rule_for_Trend_origin_when_a_source_is_a_valid_URL()
    {
        var validator = new ContentValidator();

        var result = validator.Validate(Draft(sources: ["https://dev.to/some-trend"]), ContentOrigin.Trend, [], ReferenceDate);

        result.FailedRules.Should().NotContain(rule => rule.Contains("valid URL"));
    }

    [Fact]
    public void Validate_adds_a_warning_but_stays_valid_when_topics_overlap_significantly_with_a_recent_post()
    {
        var validator = new ContentValidator();
        var recentPostId = Guid.NewGuid();
        var recentPosts = new[]
        {
            new RecentPostTopics(recentPostId, ["dotnet", "postgresql"], ReferenceDate.AddDays(-2))
        };

        var result = validator.Validate(Draft(hashtags: ["#dotnet", "#postgresql"]), ContentOrigin.GitHub, recentPosts, ReferenceDate);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle(warning => warning.Contains(recentPostId.ToString()));
    }

    [Fact]
    public void Validate_adds_no_warning_when_there_is_no_topic_overlap_with_recent_posts()
    {
        var validator = new ContentValidator();
        var recentPosts = new[]
        {
            new RecentPostTopics(Guid.NewGuid(), ["kubernetes", "kafka"], ReferenceDate.AddDays(-1))
        };

        var result = validator.Validate(Draft(hashtags: ["#dotnet", "#postgresql"]), ContentOrigin.GitHub, recentPosts, ReferenceDate);

        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_reports_every_failed_rule_at_once_when_several_checks_fail_together()
    {
        var validator = new ContentValidator();
        var draft = Draft(totalLength: 50, hashtags: ["#motivation"], sources: []);

        var result = validator.Validate(draft, ContentOrigin.Trend, [], ReferenceDate);

        result.IsValid.Should().BeFalse();
        result.FailedRules.Should().Contain(rule => rule.Contains("expected between 700 and 1600"));
        result.FailedRules.Should().Contain(rule => rule.Contains("whitelist"));
        result.FailedRules.Should().Contain(rule => rule.Contains("traceable to at least one real source"));
        result.FailedRules.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_throws_when_max_length_is_less_than_min_length()
    {
        var act = () => new ContentValidator(minLength: 1000, maxLength: 500);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
