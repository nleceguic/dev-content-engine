using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Services;
using DevContentEngine.Domain.Entities;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.Services;

public class RepoHighlightSelectorTests
{
    private static readonly DateTime Now = new(2026, 8, 12, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IGitHubRepositoryRepository> _repositoryRepository = new();
    private readonly Mock<IGeneratedPostRepository> _generatedPostRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public RepoHighlightSelectorTests()
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);
    }

    private RepoHighlightSelector CreateSelector() =>
        new(_repositoryRepository.Object, _generatedPostRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task SelectAsync_returns_null_when_there_are_no_active_repositories()
    {
        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitHubRepository>());

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task SelectAsync_excludes_repositories_highlighted_within_the_last_30_days()
    {
        var recentlyHighlighted = new GitHubRepository(Guid.NewGuid(), "nleceguic", "recently-highlighted", Now.AddDays(-100));
        var eligible = new GitHubRepository(Guid.NewGuid(), "nleceguic", "eligible", Now.AddDays(-100));

        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recentlyHighlighted, eligible]);

        _generatedPostRepository
            .Setup(repository => repository.GetLastRepoHighlightByRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTime> { [recentlyHighlighted.Id] = Now.AddDays(-5) });

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(eligible.Id);
    }

    [Fact]
    public async Task SelectAsync_includes_a_repository_highlighted_exactly_30_days_ago_as_eligible_again()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "repo", Now.AddDays(-100));

        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repository]);

        _generatedPostRepository
            .Setup(repository => repository.GetLastRepoHighlightByRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTime> { [repository.Id] = Now.AddDays(-31) });

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(repository.Id);
    }

    [Fact]
    public async Task SelectAsync_returns_null_when_every_active_repository_was_highlighted_recently()
    {
        var repositoryA = new GitHubRepository(Guid.NewGuid(), "nleceguic", "a", Now.AddDays(-100));
        var repositoryB = new GitHubRepository(Guid.NewGuid(), "nleceguic", "b", Now.AddDays(-100));

        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repositoryA, repositoryB]);

        _generatedPostRepository
            .Setup(repository => repository.GetLastRepoHighlightByRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTime>
            {
                [repositoryA.Id] = Now.AddDays(-1),
                [repositoryB.Id] = Now.AddDays(-10)
            });

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task SelectAsync_prefers_a_repository_that_was_never_highlighted_over_one_highlighted_long_ago()
    {
        var neverHighlighted = new GitHubRepository(Guid.NewGuid(), "nleceguic", "never", Now.AddDays(-10));
        var highlightedLongAgo = new GitHubRepository(Guid.NewGuid(), "nleceguic", "long-ago", Now.AddDays(-200));

        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([highlightedLongAgo, neverHighlighted]);

        _generatedPostRepository
            .Setup(repository => repository.GetLastRepoHighlightByRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTime> { [highlightedLongAgo.Id] = Now.AddDays(-60) });

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(neverHighlighted.Id);
    }

    [Fact]
    public async Task SelectAsync_picks_the_repository_highlighted_longest_ago_among_eligible_ones()
    {
        var highlightedRecently = new GitHubRepository(Guid.NewGuid(), "nleceguic", "recent", Now.AddDays(-200));
        var highlightedLongestAgo = new GitHubRepository(Guid.NewGuid(), "nleceguic", "oldest-highlight", Now.AddDays(-200));

        _repositoryRepository
            .Setup(repository => repository.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([highlightedRecently, highlightedLongestAgo]);

        _generatedPostRepository
            .Setup(repository => repository.GetLastRepoHighlightByRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTime>
            {
                [highlightedRecently.Id] = Now.AddDays(-35),
                [highlightedLongestAgo.Id] = Now.AddDays(-90)
            });

        var selector = CreateSelector();

        var result = await selector.SelectAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(highlightedLongestAgo.Id);
    }
}
