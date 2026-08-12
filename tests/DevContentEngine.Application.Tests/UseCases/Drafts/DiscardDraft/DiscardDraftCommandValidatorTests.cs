using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Application.UseCases.Drafts.DiscardDraft;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.Drafts.DiscardDraft;

public class DiscardDraftCommandValidatorTests
{
    private readonly Mock<IGeneratedPostRepository> _repository = new();

    private DiscardDraftCommandValidator CreateValidator() => new(_repository.Object);

    [Theory]
    [InlineData(GeneratedPostStatus.Draft)]
    [InlineData(GeneratedPostStatus.Edited)]
    [InlineData(GeneratedPostStatus.Discarded)]
    public async Task Validate_succeeds_when_the_draft_has_not_been_used(GeneratedPostStatus status)
    {
        var draft = GeneratedPostTestFactory.Create(status);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var result = await CreateValidator().ValidateAsync(new DiscardDraftCommand(draft.Id));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_fails_when_the_draft_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedPost?)null);

        var result = await CreateValidator().ValidateAsync(new DiscardDraftCommand(missingId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("does not exist"));
    }

    [Fact]
    public async Task Validate_fails_when_the_draft_was_already_used()
    {
        var draft = GeneratedPostTestFactory.Create(GeneratedPostStatus.Used);
        _repository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var result = await CreateValidator().ValidateAsync(new DiscardDraftCommand(draft.Id));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("cannot be discarded"));
    }
}
