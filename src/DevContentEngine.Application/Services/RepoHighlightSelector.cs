using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Services;

public sealed class RepoHighlightSelector : IRepoHighlightSelector
{
    private const int RecentHighlightLookbackDays = 30;

    private readonly IGitHubRepositoryRepository _repositoryRepository;
    private readonly IGeneratedPostRepository _generatedPostRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RepoHighlightSelector(
        IGitHubRepositoryRepository repositoryRepository,
        IGeneratedPostRepository generatedPostRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _repositoryRepository = repositoryRepository;
        _generatedPostRepository = generatedPostRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GitHubRepository?> SelectAsync(CancellationToken cancellationToken = default)
    {
        var activeRepositories = await _repositoryRepository.GetActiveRepositoriesAsync(cancellationToken);

        if (activeRepositories.Count == 0)
        {
            return null;
        }

        var lastHighlightedByRepository = await _generatedPostRepository.GetLastRepoHighlightByRepositoryAsync(cancellationToken);
        var cutoff = _dateTimeProvider.UtcNow.AddDays(-RecentHighlightLookbackDays);

        var eligibleRepositories = activeRepositories
            .Where(repository => !WasHighlightedRecently(repository.Id, lastHighlightedByRepository, cutoff))
            .ToList();

        if (eligibleRepositories.Count == 0)
        {
            return null;
        }

        return eligibleRepositories
            .OrderBy(repository => LastHighlightedAtOrNever(repository.Id, lastHighlightedByRepository))
            .ThenBy(repository => repository.DetectedAt)
            .First();
    }

    private static bool WasHighlightedRecently(
        Guid repositoryId,
        IReadOnlyDictionary<Guid, DateTime> lastHighlightedByRepository,
        DateTime cutoff)
    {
        return lastHighlightedByRepository.TryGetValue(repositoryId, out var lastHighlightedAt) && lastHighlightedAt >= cutoff;
    }

    private static DateTime LastHighlightedAtOrNever(
        Guid repositoryId,
        IReadOnlyDictionary<Guid, DateTime> lastHighlightedByRepository)
    {
        return lastHighlightedByRepository.TryGetValue(repositoryId, out var lastHighlightedAt) ? lastHighlightedAt : DateTime.MinValue;
    }
}
