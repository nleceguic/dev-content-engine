namespace DevContentEngine.Domain.Services;

public sealed record TopicRepetitionResult
{
    public bool SimilarToRecentPost { get; }
    public decimal Penalty { get; }
    public Guid? MostSimilarPostId { get; }
    public double? HighestOverlapRatio { get; }

    public TopicRepetitionResult(bool similarToRecentPost, decimal penalty, Guid? mostSimilarPostId, double? highestOverlapRatio)
    {
        SimilarToRecentPost = similarToRecentPost;
        Penalty = penalty;
        MostSimilarPostId = mostSimilarPostId;
        HighestOverlapRatio = highestOverlapRatio;
    }

    public static TopicRepetitionResult None { get; } = new(false, 0m, null, null);
}
