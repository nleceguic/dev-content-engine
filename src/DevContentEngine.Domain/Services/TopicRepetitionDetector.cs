namespace DevContentEngine.Domain.Services;

public sealed class TopicRepetitionDetector
{
    private readonly TopicRepetitionOptions _options;

    public TopicRepetitionDetector(TopicRepetitionOptions? options = null)
    {
        _options = options ?? TopicRepetitionOptions.Default;
    }

    public TopicRepetitionResult Detect(
        IEnumerable<string> candidateKeywords,
        IEnumerable<RecentPostTopics> recentPosts,
        DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(candidateKeywords);
        ArgumentNullException.ThrowIfNull(recentPosts);

        var candidateSet = new HashSet<string>(
            candidateKeywords.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).Select(keyword => keyword.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var windowStart = referenceDate - _options.LookbackWindow;

        var relevantPosts = recentPosts.Where(post => post.PublishedAt >= windowStart && post.PublishedAt <= referenceDate);

        Guid? mostSimilarPostId = null;
        var highestOverlap = 0.0;

        foreach (var post in relevantPosts)
        {
            var overlap = CalculateOverlap(candidateSet, post.Keywords);

            if (overlap > highestOverlap)
            {
                highestOverlap = overlap;
                mostSimilarPostId = post.PostId;
            }
        }

        if (mostSimilarPostId is null || highestOverlap < _options.OverlapThreshold)
            return TopicRepetitionResult.None;

        return new TopicRepetitionResult(true, _options.Penalty, mostSimilarPostId, highestOverlap);
    }

    private static double CalculateOverlap(HashSet<string> candidateSet, IReadOnlyCollection<string> postKeywords)
    {
        var postSet = new HashSet<string>(postKeywords, StringComparer.OrdinalIgnoreCase);

        if (candidateSet.Count == 0 || postSet.Count == 0)
            return 0;

        var intersectionCount = candidateSet.Count(postSet.Contains);
        var unionCount = candidateSet.Union(postSet, StringComparer.OrdinalIgnoreCase).Count();

        return (double)intersectionCount / unionCount;
    }
}
