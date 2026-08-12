using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.EditDraft;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.EditDraft;

public class EditDraftCommandValidatorTests
{
    private readonly Mock<IGeneratedPostRepository> _repository = new();

    private EditDraftCommandValidator CreateValidator() => new(_repository.Object);

    private static EditDraftCommand CommandFor(Guid draftId) =>
        new(draftId, "Hook", "Body", "Conclusion", "Cta", ["#dotnet"]);

    [Theory]
    [InlineData(GeneratedPostStatus.Draft)]
    [InlineData(GeneratedPostStatus.Edited)]
    public async Task Validate_succeeds_for_a_valid_transition(GeneratedPostStatus status)
    {
        var draft = GeneratedPostTestFactory.Create(status);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var result = await CreateValidator().ValidateAsync(CommandFor(draft.Id));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_fails_when_the_draft_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedPost?)null);

        var result = await CreateValidator().ValidateAsync(CommandFor(missingId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(EditDraftCommand.DraftId) && error.ErrorMessage.Contains("does not exist"));
    }

    [Theory]
    [InlineData(GeneratedPostStatus.Used)]
    [InlineData(GeneratedPostStatus.Discarded)]
    public async Task Validate_fails_for_an_invalid_transition_from_a_terminal_status(GeneratedPostStatus status)
    {
        var draft = GeneratedPostTestFactory.Create(status);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var result = await CreateValidator().ValidateAsync(CommandFor(draft.Id));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(EditDraftCommand.DraftId) && error.ErrorMessage.Contains("cannot be edited"));
    }

    [Fact]
    public async Task Validate_fails_when_the_new_text_fields_are_empty()
    {
        var draft = GeneratedPostTestFactory.Create();
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var command = new EditDraftCommand(draft.Id, string.Empty, string.Empty, string.Empty, null, []);

        var result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(EditDraftCommand.Hook));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(EditDraftCommand.Body));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(EditDraftCommand.Conclusion));
    }
}
