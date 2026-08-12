using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace DevContentEngine.Application.UseCases.Drafts.ApproveDraft;

public sealed class ApproveDraftCommandValidator : AbstractValidator<ApproveDraftCommand>
{
    private readonly IGeneratedPostRepository _repository;

    public ApproveDraftCommandValidator(IGeneratedPostRepository repository)
    {
        _repository = repository;

        RuleFor(command => command).CustomAsync(ValidateDraftAsync);
    }

    private async Task ValidateDraftAsync(ApproveDraftCommand command, ValidationContext<ApproveDraftCommand> context, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(command.DraftId, cancellationToken);

        if (draft is null)
        {
            context.AddFailure(new ValidationFailure(nameof(ApproveDraftCommand.DraftId), $"Draft '{command.DraftId}' does not exist."));
            return;
        }

        if (draft.Status is GeneratedPostStatus.Used or GeneratedPostStatus.Discarded)
        {
            context.AddFailure(new ValidationFailure(
                nameof(ApproveDraftCommand.DraftId),
                $"Draft '{command.DraftId}' cannot be approved from status '{draft.Status}'."));
        }
    }
}
