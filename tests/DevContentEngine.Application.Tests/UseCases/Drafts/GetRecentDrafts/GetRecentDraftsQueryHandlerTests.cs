using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.GetRecentDrafts;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.GetRecentDrafts;

public class GetRecentDraftsQueryHandlerTests
{
    [Fact]
    public async Task Handle_maps_the_repositorys_latest_drafts_to_dtos_preserving_order()
    {
        var drafts = new[]
        {
            GeneratedPostTestFactory.Create(),
            GeneratedPostTestFactory.Create(GeneratedPostStatus.Edited),
            GeneratedPostTestFactory.Create(GeneratedPostStatus.Used)
        };

        var repository = new Mock<IGeneratedPostRepository>();
        repository.Setup(r => r.GetLatestAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(drafts);

        var handler = new GetRecentDraftsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRecentDraftsQuery(3), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(dto => dto.Id).Should().ContainInOrder(drafts.Select(draft => draft.Id));
        result.Select(dto => dto.Status).Should().BeEquivalentTo(drafts.Select(draft => draft.Status));
    }

    [Fact]
    public async Task Handle_returns_an_empty_collection_when_there_are_no_drafts()
    {
        var repository = new Mock<IGeneratedPostRepository>();
        repository.Setup(r => r.GetLatestAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = new GetRecentDraftsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRecentDraftsQuery(5), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
