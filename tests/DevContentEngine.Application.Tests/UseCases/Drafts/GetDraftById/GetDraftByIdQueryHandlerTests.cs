using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.GetDraftById;
using DevContentEngine.Domain.Entities;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.GetDraftById;

public class GetDraftByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_draft_when_it_exists()
    {
        var draft = GeneratedPostTestFactory.Create();

        var repository = new Mock<IGeneratedPostRepository>();
        repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var handler = new GetDraftByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetDraftByIdQuery(draft.Id), CancellationToken.None);

        result.Id.Should().Be(draft.Id);
        result.Hook.Should().Be(draft.Hook);
        result.Body.Should().Be(draft.Body);
        result.Status.Should().Be(draft.Status);
        result.Sources.Should().BeEquivalentTo(draft.Sources);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_draft_does_not_exist()
    {
        var missingId = Guid.NewGuid();

        var repository = new Mock<IGeneratedPostRepository>();
        repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedPost?)null);

        var handler = new GetDraftByIdQueryHandler(repository.Object);

        var act = async () => await handler.Handle(new GetDraftByIdQuery(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
