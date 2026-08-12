using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using MediatR;

namespace DevContentEngine.Application.UseCases.GenerateDailyContent;

public sealed class GenerateDailyContentCommandHandler : IRequestHandler<GenerateDailyContentCommand, GenerateDailyContentResult>
{
    private static readonly TimeSpan DefaultLookbackWindow = TimeSpan.FromDays(1);
    private const int TopicRepetitionLookbackDays = 14;
    private static readonly string[] TrendKeywords = ["backend", ".net", "dotnet", "c#", "csharp"];

    private readonly IGitHubClient _gitHubClient;
    private readonly ITrendSource _trendSource;
    private readonly IContentGenerationService _contentGenerationService;
    private readonly IGitHubRepositoryRepository _repositoryRepository;
    private readonly IGitHubActivityRepository _activityRepository;
    private readonly IContentIdeaRepository _contentIdeaRepository;
    private readonly IGeneratedPostRepository _generatedPostRepository;
    private readonly IGenerationRunRepository _generationRunRepository;
    private readonly ITrendRepository _trendRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ActivityNoiseFilter _noiseFilter;
    private readonly TechnologyExtractor _technologyExtractor;
    private readonly ActivityScorer _activityScorer;
    private readonly TopicRepetitionDetector _topicRepetitionDetector;

    public GenerateDailyContentCommandHandler(
        IGitHubClient gitHubClient,
        ITrendSource trendSource,
        IContentGenerationService contentGenerationService,
        IGitHubRepositoryRepository repositoryRepository,
        IGitHubActivityRepository activityRepository,
        IContentIdeaRepository contentIdeaRepository,
        IGeneratedPostRepository generatedPostRepository,
        IGenerationRunRepository generationRunRepository,
        ITrendRepository trendRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDateTimeProvider dateTimeProvider,
        ActivityNoiseFilter noiseFilter,
        TechnologyExtractor technologyExtractor,
        ActivityScorer activityScorer,
        TopicRepetitionDetector topicRepetitionDetector)
    {
        _gitHubClient = gitHubClient;
        _trendSource = trendSource;
        _contentGenerationService = contentGenerationService;
        _repositoryRepository = repositoryRepository;
        _activityRepository = activityRepository;
        _contentIdeaRepository = contentIdeaRepository;
        _generatedPostRepository = generatedPostRepository;
        _generationRunRepository = generationRunRepository;
        _trendRepository = trendRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
        _dateTimeProvider = dateTimeProvider;
        _noiseFilter = noiseFilter;
        _technologyExtractor = technologyExtractor;
        _activityScorer = activityScorer;
        _topicRepetitionDetector = topicRepetitionDetector;
    }

    public async Task<GenerateDailyContentResult> Handle(GenerateDailyContentCommand request, CancellationToken cancellationToken)
    {
        var startedAt = _dateTimeProvider.UtcNow;
        var generationRun = new GenerationRun(Guid.NewGuid(), startedAt);
        var trackedEntities = new List<Entity> { generationRun };

        await _generationRunRepository.AddAsync(generationRun, cancellationToken);

        try
        {
            var sinceUtc = await _generationRunRepository.GetLastRunTimestampAsync(cancellationToken)
                ?? startedAt - DefaultLookbackWindow;

            GitHubUserActivity rawActivity;

            try
            {
                rawActivity = await _gitHubClient.GetUserActivityAsync(request.GitHubUsername, sinceUtc, cancellationToken);
            }
            catch (Exception ex)
            {
                return await CompleteAsFailedAsync(generationRun, $"Failed to retrieve GitHub activity: {ex.Message}", trackedEntities, cancellationToken);
            }

            var repositories = await ResolveRepositoriesAsync(rawActivity, startedAt, cancellationToken);
            trackedEntities.AddRange(repositories.Values);

            var activities = await BuildGitHubActivitiesAsync(rawActivity, repositories, cancellationToken);
            trackedEntities.AddRange(activities);

            var groupedCommitCount = activities.Count(activity => activity.Type == GitHubActivityType.Commit);

            var closedIssuesWithContext = rawActivity.Issues.Count(issue => !string.IsNullOrWhiteSpace(issue.ClosingContext));
            var detectedTechnologies = activities
                .SelectMany(activity => activity.DetectedTechnologies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var scoringInput = new ActivityScoringInput(
                relevantCommits: groupedCommitCount,
                mergedPullRequests: rawActivity.PullRequests.Count,
                closedIssuesWithContext: closedIssuesWithContext,
                newRepositoriesDetected: rawActivity.NewRepositories.Count,
                technologicalDiversity: detectedTechnologies.Count);

            var recentPosts = await _generatedPostRepository.GetRecentPostsAsync(TopicRepetitionLookbackDays, cancellationToken);
            var recentPostTopics = recentPosts
                .Select(post => new RecentPostTopics(post.Id, post.Hashtags, post.CreatedAt))
                .ToList();

            var repetition = _topicRepetitionDetector.Detect(detectedTechnologies, recentPostTopics, startedAt);
            var scoreResult = _activityScorer.Score(scoringInput, repetition.Penalty);

            ContentIdea contentIdea;
            IReadOnlyCollection<string> sourceReferences;
            Trend? trend = null;

            if (scoreResult.ChosenPath == ContentPath.GitHubPath)
            {
                contentIdea = new ContentIdea(
                    Guid.NewGuid(),
                    ContentOrigin.GitHub,
                    scoreResult.Score,
                    activities.Select(activity => activity.Id),
                    relatedTrendId: null,
                    startedAt,
                    ContentPath.GitHubPath);

                sourceReferences = activities.Select(activity => activity.ExternalId).ToList();
            }
            else
            {
                var trendCandidates = await _trendSource.GetTrendsAsync(TrendKeywords, cancellationToken);

                if (trendCandidates.Count == 0)
                {
                    return await CompleteAsNoContentAsync(
                        generationRun,
                        "No trend candidates were found for the configured keywords.",
                        trackedEntities,
                        cancellationToken);
                }

                var selectedCandidate = trendCandidates.First();
                trend = new Trend(
                    Guid.NewGuid(),
                    selectedCandidate.Title,
                    selectedCandidate.Source,
                    selectedCandidate.Url,
                    selectedCandidate.PublishedAt,
                    relevanceScore: 1.0m);

                await _trendRepository.AddAsync(trend, cancellationToken);
                trackedEntities.Add(trend);

                contentIdea = new ContentIdea(
                    Guid.NewGuid(),
                    ContentOrigin.Trend,
                    scoreResult.Score,
                    relatedActivityIds: [],
                    relatedTrendId: trend.Id,
                    startedAt,
                    ContentPath.TrendPath);

                sourceReferences = [trend.Url];
            }

            await _contentIdeaRepository.AddAsync(contentIdea, cancellationToken);
            trackedEntities.Add(contentIdea);

            var generationResult = await _contentGenerationService.GenerateAsync(
                new ContentGenerationRequest(
                    contentIdea,
                    activities,
                    repositories.Values.ToList(),
                    trend,
                    sourceReferences,
                    recentPostTopics,
                    startedAt),
                cancellationToken);

            if (generationResult is null)
            {
                return await CompleteAsNoContentAsync(
                    generationRun,
                    "Content generation did not produce a post that passed validation.",
                    trackedEntities,
                    cancellationToken);
            }

            var generatedPost = new GeneratedPost(
                Guid.NewGuid(),
                contentIdea.Id,
                generationResult.Hook,
                generationResult.Body,
                generationResult.Conclusion,
                generationResult.Cta,
                generationResult.Hashtags,
                generationResult.Sources,
                contentIdea.Origin,
                generationResult.PromptVersionId,
                startedAt);

            await _generatedPostRepository.AddAsync(generatedPost, cancellationToken);
            trackedEntities.Add(generatedPost);

            generationRun.CompleteSuccessfully(scoreResult.ChosenPath, generatedPost.Id, _dateTimeProvider.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _domainEventDispatcher.DispatchAndClearEventsAsync(trackedEntities, cancellationToken);

            return new GenerateDailyContentResult(GenerationRunStatus.Success, generationRun.Id, generatedPost.Id, null);
        }
        catch (Exception ex)
        {
            return await CompleteAsFailedAsync(generationRun, ex.Message, trackedEntities, cancellationToken);
        }
    }

    private async Task<Dictionary<(string Owner, string Name), GitHubRepository>> ResolveRepositoriesAsync(
        GitHubUserActivity activity,
        DateTime detectedAt,
        CancellationToken cancellationToken)
    {
        var keys = activity.Commits.Select(commit => (commit.RepositoryOwner, commit.RepositoryName))
            .Concat(activity.PullRequests.Select(pr => (pr.RepositoryOwner, pr.RepositoryName)))
            .Concat(activity.Issues.Select(issue => (issue.RepositoryOwner, issue.RepositoryName)))
            .Concat(activity.NewRepositories.Select(repo => (repo.Owner, repo.Name)))
            .Distinct();

        var resolved = new Dictionary<(string, string), GitHubRepository>();

        foreach (var (owner, name) in keys)
        {
            var repository = await _repositoryRepository.GetByOwnerAndNameAsync(owner, name, cancellationToken);

            if (repository is null)
            {
                repository = new GitHubRepository(Guid.NewGuid(), owner, name, detectedAt);
                await _repositoryRepository.AddAsync(repository, cancellationToken);
            }

            resolved[(owner, name)] = repository;
        }

        return resolved;
    }

    private static List<CommitInfo> BuildCommitInfos(
        GitHubUserActivity activity,
        IReadOnlyDictionary<(string Owner, string Name), GitHubRepository> repositories)
    {
        return activity.Commits
            .Select(commit => new CommitInfo(
                repositories[(commit.RepositoryOwner, commit.RepositoryName)].Id,
                commit.Sha,
                commit.Message,
                commit.CommittedAt,
                commit.FilesChanged,
                commit.LinesChanged))
            .ToList();
    }

    private async Task<List<GitHubActivity>> BuildGitHubActivitiesAsync(
        GitHubUserActivity activity,
        IReadOnlyDictionary<(string Owner, string Name), GitHubRepository> repositories,
        CancellationToken cancellationToken)
    {
        var commitInfos = BuildCommitInfos(activity, repositories);
        var groupedCommits = _noiseFilter.FilterAndGroup(commitInfos);

        var activities = new List<GitHubActivity>();

        foreach (var group in groupedCommits)
        {
            var filePaths = activity.Commits
                .Where(commit => group.CommitExternalIds.Contains(commit.Sha))
                .SelectMany(commit => commit.ChangedFilePaths);

            var technologies = _technologyExtractor.Extract(filePaths, group.Messages);

            var groupActivity = new GitHubActivity(
                Guid.NewGuid(),
                group.RepositoryId,
                GitHubActivityType.Commit,
                technologies,
                summary: string.Join("; ", group.Messages),
                externalId: $"{group.RepositoryId}:{group.Date:yyyy-MM-dd}",
                timestamp: group.LatestTimestamp);

            groupActivity.MarkAnalyzed(isNoise: false);
            activities.Add(groupActivity);
        }

        foreach (var pullRequest in activity.PullRequests)
        {
            var technologies = _technologyExtractor.Extract(pullRequest.ChangedFilePaths, [pullRequest.Title]);

            var pullRequestActivity = new GitHubActivity(
                Guid.NewGuid(),
                repositories[(pullRequest.RepositoryOwner, pullRequest.RepositoryName)].Id,
                GitHubActivityType.PullRequest,
                technologies,
                summary: pullRequest.Title,
                externalId: pullRequest.ExternalId,
                timestamp: pullRequest.MergedAt);

            pullRequestActivity.MarkAnalyzed(isNoise: false);
            activities.Add(pullRequestActivity);
        }

        foreach (var issue in activity.Issues)
        {
            var messages = new List<string> { issue.Title };

            if (!string.IsNullOrWhiteSpace(issue.ClosingContext))
            {
                messages.Add(issue.ClosingContext);
            }

            var technologies = _technologyExtractor.Extract([], messages);
            var summary = string.IsNullOrWhiteSpace(issue.ClosingContext)
                ? issue.Title
                : $"{issue.Title} — {issue.ClosingContext}";

            var issueActivity = new GitHubActivity(
                Guid.NewGuid(),
                repositories[(issue.RepositoryOwner, issue.RepositoryName)].Id,
                GitHubActivityType.Issue,
                technologies,
                summary,
                externalId: issue.ExternalId,
                timestamp: issue.ClosedAt);

            issueActivity.MarkAnalyzed(isNoise: false);
            activities.Add(issueActivity);
        }

        foreach (var newRepository in activity.NewRepositories)
        {
            var newRepositoryActivity = new GitHubActivity(
                Guid.NewGuid(),
                repositories[(newRepository.Owner, newRepository.Name)].Id,
                GitHubActivityType.NewRepository,
                detectedTechnologies: [],
                summary: $"New repository {newRepository.Owner}/{newRepository.Name}",
                externalId: $"{newRepository.Owner}/{newRepository.Name}",
                timestamp: newRepository.CreatedAt);

            newRepositoryActivity.MarkAnalyzed(isNoise: false);
            activities.Add(newRepositoryActivity);
        }

        if (activities.Count > 0)
        {
            await _activityRepository.AddRangeAsync(activities, cancellationToken);
        }

        return activities;
    }

    private async Task<GenerateDailyContentResult> CompleteAsFailedAsync(
        GenerationRun generationRun,
        string errorMessage,
        IReadOnlyCollection<Entity> trackedEntities,
        CancellationToken cancellationToken)
    {
        generationRun.Fail(errorMessage, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAndClearEventsAsync(trackedEntities, cancellationToken);

        return new GenerateDailyContentResult(GenerationRunStatus.Failed, generationRun.Id, null, errorMessage);
    }

    private async Task<GenerateDailyContentResult> CompleteAsNoContentAsync(
        GenerationRun generationRun,
        string reason,
        IReadOnlyCollection<Entity> trackedEntities,
        CancellationToken cancellationToken)
    {
        generationRun.CompleteWithoutContent(_dateTimeProvider.UtcNow, reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventDispatcher.DispatchAndClearEventsAsync(trackedEntities, cancellationToken);

        return new GenerateDailyContentResult(GenerationRunStatus.NoContentGenerated, generationRun.Id, null, reason);
    }
}
