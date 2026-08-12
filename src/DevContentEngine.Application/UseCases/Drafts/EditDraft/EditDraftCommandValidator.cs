using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace DevContentEngine.Application.UseCases.Drafts.EditDraft;

public sealed class EditDraftCommandValidator : AbstractValidator<EditDraftCommand>
{
    private readonly IGeneratedPostRepository _repository;

    public EditDraftCommandValidator(IGeneratedPostRepository repository)
    {
        _repository = repository;

        RuleFor(command => command.Hook).NotEmpty();
        RuleFor(command => command.Body).NotEmpty();
        RuleFor(command => command.Conclusion).NotEmpty();

        RuleFor(command => command).CustomAsync(ValidateDraftAsync);
    }

    private async Task ValidateDraftAsync(EditDraftCommand command, ValidationContext<EditDraftCommand> context, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(command.DraftId, cancellationToken);

        if (draft is null)
        {
            context.AddFailure(new ValidationFailure(nameof(EditDraftCommand.DraftId), $"Draft '{command.DraftId}' does not exist."));
            return;
        }

        if (draft.Status is GeneratedPostStatus.Used or GeneratedPostStatus.Discarded)
        {
            context.AddFailure(new ValidationFailure(
                nameof(EditDraftCommand.DraftId),
                $"Draft '{command.DraftId}' cannot be edited from status '{draft.Status}'."));
        }
    }
}
