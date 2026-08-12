using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.ApproveDraft;

public sealed class ApproveDraftCommandHandler : IRequestHandler<ApproveDraftCommand, GeneratedPostDto>
{
    private readonly IGeneratedPostRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveDraftCommandHandler(
        IGeneratedPostRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GeneratedPostDto> Handle(ApproveDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(request.DraftId, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratedPost), request.DraftId);

        draft.MarkAsUsed(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GeneratedPostDto.FromEntity(draft);
    }
}
