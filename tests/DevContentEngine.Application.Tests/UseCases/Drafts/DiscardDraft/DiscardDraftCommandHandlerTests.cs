using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.DiscardDraft;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.DiscardDraft;

public class DiscardDraftCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IGeneratedPostRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public DiscardDraftCommandHandlerTests()
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);
    }

    private DiscardDraftCommandHandler CreateHandler() =>
        new(_repository.Object, _unitOfWork.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task Handle_marks_the_draft_as_Discarded()
    {
        var draft = GeneratedPostTestFactory.Create();
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var result = await CreateHandler().Handle(new DiscardDraftCommand(draft.Id), CancellationToken.None);

        result.Status.Should().Be(GeneratedPostStatus.Discarded);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_draft_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedPost?)null);

        var act = async () => await CreateHandler().Handle(new DiscardDraftCommand(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_draft_was_already_used()
    {
        var draft = GeneratedPostTestFactory.Create(GeneratedPostStatus.Used);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var act = async () => await CreateHandler().Handle(new DiscardDraftCommand(draft.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
