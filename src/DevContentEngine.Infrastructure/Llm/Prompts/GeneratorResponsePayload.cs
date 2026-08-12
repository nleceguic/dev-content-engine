using System.Text.Json.Serialization;

namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal sealed record GeneratorResponsePayload(
    [property: JsonPropertyName("hook")] string Hook,
    [property: JsonPropertyName("cuerpo")] string Cuerpo,
    [property: JsonPropertyName("conclusion")] string Conclusion,
    [property: JsonPropertyName("cta")] string? Cta,
    [property: JsonPropertyName("hashtags")] IReadOnlyCollection<string> Hashtags,
    [property: JsonPropertyName("fuentes")] IReadOnlyCollection<string> Fuentes);
