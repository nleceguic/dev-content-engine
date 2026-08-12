using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Domain.Services;

public sealed class ContentValidator
{
    public const int DefaultMinLength = 700;
    public const int DefaultMaxLength = 1600;
    public const int MaxHashtags = 5;

    private readonly int _minLength;
    private readonly int _maxLength;
    private readonly IReadOnlyCollection<string> _technologyWhitelist;
    private readonly IReadOnlyCollection<string> _bannedPhrases;
    private readonly TopicRepetitionDetector _topicRepetitionDetector;

    public ContentValidator(
        int minLength = DefaultMinLength,
        int maxLength = DefaultMaxLength,
        IEnumerable<string>? technologyWhitelist = null,
        IEnumerable<string>? bannedPhrases = null,
        TopicRepetitionDetector? topicRepetitionDetector = null)
    {
        if (minLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "MinLength must be positive.");

        if (maxLength < minLength)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "MaxLength cannot be less than MinLength.");

        _minLength = minLength;
        _maxLength = maxLength;
        _technologyWhitelist = (technologyWhitelist ?? DefaultTechnologyHashtagWhitelist.Hashtags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.TrimStart('#'))
            .ToList();
        _bannedPhrases = (bannedPhrases ?? DefaultBannedPhrases.Phrases)
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .ToList();
        _topicRepetitionDetector = topicRepetitionDetector ?? new TopicRepetitionDetector();
    }

    public ValidationResult Validate(
        DraftContent draft,
        ContentOrigin origin,
        IEnumerable<RecentPostTopics> recentPosts,
        DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(recentPosts);

        var failedRules = new List<string>();
        var warnings = new List<string>();

        var hook = draft.Hook ?? string.Empty;
        var body = draft.Body ?? string.Empty;
        var conclusion = draft.Conclusion ?? string.Empty;
        var cta = draft.Cta ?? string.Empty;
        var diagrama = draft.Diagrama ?? string.Empty;

        ValidateLength(hook, body, conclusion, cta, failedRules);
        ValidateHashtags(draft.Hashtags, failedRules);
        ValidateBannedPhrases(hook, body, conclusion, cta, failedRules);
        ValidateSources(draft.Sources, origin, failedRules);
        ValidateDiagram(diagrama, failedRules);
        ValidateTopicRepetition(draft.Hashtags, recentPosts, referenceDate, warnings);

        return new ValidationResult(failedRules, warnings);
    }

    private void ValidateLength(string hook, string body, string conclusion, string cta, List<string> failedRules)
    {
        var totalLength = hook.Length + body.Length + conclusion.Length + cta.Length;

        if (totalLength < _minLength || totalLength > _maxLength)
        {
            failedRules.Add(
                $"Total content length is {totalLength} characters; expected between {_minLength} and {_maxLength}.");
        }
    }

    private void ValidateHashtags(IReadOnlyCollection<string> hashtags, List<string> failedRules)
    {
        if (hashtags.Count > MaxHashtags)
        {
            failedRules.Add($"Post has {hashtags.Count} hashtags; maximum allowed is {MaxHashtags}.");
        }

        var unknownHashtags = hashtags
            .Where(tag => !_technologyWhitelist.Contains(tag.TrimStart('#'), StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (unknownHashtags.Count > 0)
        {
            failedRules.Add($"Hashtags not in the known technology whitelist: {string.Join(", ", unknownHashtags)}.");
        }
    }

    private void ValidateBannedPhrases(string hook, string body, string conclusion, string cta, List<string> failedRules)
    {
        var fullText = string.Join(" ", hook, body, conclusion, cta);

        var matchedBannedPhrase = _bannedPhrases
            .FirstOrDefault(phrase => fullText.Contains(phrase, StringComparison.OrdinalIgnoreCase));

        if (matchedBannedPhrase is not null)
        {
            failedRules.Add($"Content uses a forbidden/clickbait phrase: \"{matchedBannedPhrase}\".");
        }
    }

    private static void ValidateSources(IReadOnlyCollection<string> sources, ContentOrigin origin, List<string> failedRules)
    {
        if (sources.Count == 0)
        {
            failedRules.Add("A generated post must be traceable to at least one real source.");
            return;
        }

        if (origin == ContentOrigin.Trend && !sources.Any(IsValidHttpUrl))
        {
            failedRules.Add("A trend-originated post must include at least one source with a valid URL.");
        }

        if (origin == ContentOrigin.RepoHighlight && !sources.Any(IsValidHttpUrl))
        {
            failedRules.Add("A repo-highlight-originated post must include at least one source with a valid URL.");
        }
    }

    private static void ValidateDiagram(string diagram, List<string> failedRules)
    {
        if (string.IsNullOrWhiteSpace(diagram))
        {
            failedRules.Add("A generated post must include a non-empty diagram description for its image prompt.");
        }
    }

    private void ValidateTopicRepetition(
        IReadOnlyCollection<string> hashtags,
        IEnumerable<RecentPostTopics> recentPosts,
        DateTime referenceDate,
        List<string> warnings)
    {
        var candidateKeywords = hashtags.Select(tag => tag.TrimStart('#'));
        var normalizedRecentPosts = recentPosts.Select(post =>
            new RecentPostTopics(post.PostId, post.Keywords.Select(keyword => keyword.TrimStart('#')), post.PublishedAt));

        var repetition = _topicRepetitionDetector.Detect(candidateKeywords, normalizedRecentPosts, referenceDate);

        if (repetition.SimilarToRecentPost)
        {
            warnings.Add(
                $"Topic overlaps significantly with a recent post (post {repetition.MostSimilarPostId}, " +
                $"overlap {repetition.HighestOverlapRatio:P0}).");
        }
    }

    private static bool IsValidHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
