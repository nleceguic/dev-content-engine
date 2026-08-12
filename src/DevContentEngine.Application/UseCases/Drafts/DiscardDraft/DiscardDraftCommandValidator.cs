using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace DevContentEngine.Application.UseCases.Drafts.DiscardDraft;

public sealed class DiscardDraftCommandValidator : AbstractValidator<DiscardDraftCommand>
{
    private readonly IGeneratedPostRepository _repository;

    public DiscardDraftCommandValidator(IGeneratedPostRepository repository)
    {
        _repository = repository;

        RuleFor(command => command).CustomAsync(ValidateDraftAsync);
    }

    private async Task ValidateDraftAsync(DiscardDraftCommand command, ValidationContext<DiscardDraftCommand> context, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(command.DraftId, cancellationToken);

        if (draft is null)
        {
            context.AddFailure(new ValidationFailure(nameof(DiscardDraftCommand.DraftId), $"Draft '{command.DraftId}' does not exist."));
            return;
        }

        if (draft.Status == GeneratedPostStatus.Used)
        {
            context.AddFailure(new ValidationFailure(
                nameof(DiscardDraftCommand.DraftId),
                $"Draft '{command.DraftId}' cannot be discarded from status '{draft.Status}'."));
        }
    }
}
