using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Application.Interfaces.External;

public interface IContentGenerationService
{
    Task<GeneratedContentResult?> GenerateAsync(ContentGenerationRequest request, CancellationToken cancellationToken = default);
}
