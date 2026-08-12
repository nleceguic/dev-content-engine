using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.GetDraftById;

public sealed class GetDraftByIdQueryHandler : IRequestHandler<GetDraftByIdQuery, GeneratedPostDto>
{
    private readonly IGeneratedPostRepository _repository;

    public GetDraftByIdQueryHandler(IGeneratedPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<GeneratedPostDto> Handle(GetDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(request.DraftId, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratedPost), request.DraftId);

        return GeneratedPostDto.FromEntity(draft);
    }
}
