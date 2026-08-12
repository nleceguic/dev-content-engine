using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.EditDraft;

public sealed class EditDraftCommandHandler : IRequestHandler<EditDraftCommand, GeneratedPostDto>
{
    private readonly IGeneratedPostRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditDraftCommandHandler(
        IGeneratedPostRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GeneratedPostDto> Handle(EditDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(request.DraftId, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratedPost), request.DraftId);

        draft.Edit(request.Hook, request.Body, request.Conclusion, request.Cta, request.Hashtags, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GeneratedPostDto.FromEntity(draft);
    }
}
