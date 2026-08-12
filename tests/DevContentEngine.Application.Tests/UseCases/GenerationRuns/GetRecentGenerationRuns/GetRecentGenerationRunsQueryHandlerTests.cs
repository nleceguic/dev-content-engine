using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.UseCases.GenerationRuns.GetRecentGenerationRuns;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.GenerationRuns.GetRecentGenerationRuns;

public class GetRecentGenerationRunsQueryHandlerTests
{
    private static readonly DateTime StartedAt = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_maps_the_repositorys_latest_runs_to_dtos_preserving_order_and_duration()
    {
        var successful = new GenerationRun(Guid.NewGuid(), StartedAt);
        successful.CompleteSuccessfully(ContentPath.GitHubPath, Guid.NewGuid(), StartedAt.AddMinutes(3));

        var failed = new GenerationRun(Guid.NewGuid(), StartedAt.AddDays(-1));
        failed.Fail("GitHub API is unavailable", StartedAt.AddDays(-1).AddMinutes(1));

        var runs = new[] { successful, failed };

        var repository = new Mock<IGenerationRunRepository>();
        repository.Setup(r => r.GetLatestAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(runs);

        var handler = new GetRecentGenerationRunsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRecentGenerationRunsQuery(2), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(dto => dto.Id).Should().ContainInOrder(runs.Select(run => run.Id));

        var successfulDto = result.Single(dto => dto.Id == successful.Id);
        successfulDto.Status.Should().Be(GenerationRunStatus.Success);
        successfulDto.ChosenPath.Should().Be(ContentPath.GitHubPath);
        successfulDto.Duration.Should().Be(TimeSpan.FromMinutes(3));
        successfulDto.ErrorMessage.Should().BeNull();

        var failedDto = result.Single(dto => dto.Id == failed.Id);
        failedDto.Status.Should().Be(GenerationRunStatus.Failed);
        failedDto.ErrorMessage.Should().Be("GitHub API is unavailable");
        failedDto.Duration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_returns_an_empty_collection_when_there_are_no_runs()
    {
        var repository = new Mock<IGenerationRunRepository>();
        repository.Setup(r => r.GetLatestAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = new GetRecentGenerationRunsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRecentGenerationRunsQuery(10), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
