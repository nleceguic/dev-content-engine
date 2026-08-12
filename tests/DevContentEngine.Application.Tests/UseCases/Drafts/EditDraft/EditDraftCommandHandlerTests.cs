using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.EditDraft;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.EditDraft;

public class EditDraftCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IGeneratedPostRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public EditDraftCommandHandlerTests()
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);
    }

    private EditDraftCommandHandler CreateHandler() =>
        new(_repository.Object, _unitOfWork.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task Handle_updates_the_text_and_moves_the_status_to_Edited()
    {
        var draft = GeneratedPostTestFactory.Create();
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var command = new EditDraftCommand(draft.Id, "New hook", "New body", "New conclusion", "New cta", ["#kafka"]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Hook.Should().Be("New hook");
        result.Body.Should().Be("New body");
        result.Conclusion.Should().Be("New conclusion");
        result.Status.Should().Be(GeneratedPostStatus.Edited);
        result.Hashtags.Should().BeEquivalentTo(["#kafka"]);

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_draft_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedPost?)null);

        var command = new EditDraftCommand(missingId, "Hook", "Body", "Conclusion", null, []);

        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_throws_when_the_draft_is_in_a_terminal_status()
    {
        var draft = GeneratedPostTestFactory.Create(GeneratedPostStatus.Discarded);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var command = new EditDraftCommand(draft.Id, "Hook", "Body", "Conclusion", null, []);

        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
