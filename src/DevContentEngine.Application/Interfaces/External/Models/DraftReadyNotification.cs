namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record DraftReadyNotification(
    Guid GeneratedPostId,
    string Topic,
    string Origin,
    string Reason,
    string Hook,
    string Body,
    string Conclusion,
    string? Cta,
    DateTime GeneratedAt);
