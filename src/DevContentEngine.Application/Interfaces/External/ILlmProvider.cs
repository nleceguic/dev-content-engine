using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Application.Interfaces.External;

public interface ILlmProvider
{
    Task<string> GenerateAsync(
        string prompt,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<T> GenerateStructuredAsync<T>(
        string systemPrompt,
        object userPayload,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default) where T : class;
}
