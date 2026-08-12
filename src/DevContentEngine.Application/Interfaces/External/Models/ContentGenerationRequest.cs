using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Services;

namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record ContentGenerationRequest(
    ContentIdea ContentIdea,
    IReadOnlyCollection<GitHubActivity> Activities,
    IReadOnlyCollection<GitHubRepository> Repositories,
    Trend? Trend,
    GitHubRepositoryDetail? RepositoryDetail,
    IReadOnlyCollection<string> SourceReferences,
    IReadOnlyCollection<RecentPostTopics> RecentPosts,
    DateTime ReferenceDate);
