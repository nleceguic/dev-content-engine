using System.Text.Json.Serialization;

namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal sealed record ReviewerResponsePayload(
    [property: JsonPropertyName("veredicto")] string Veredicto,
    [property: JsonPropertyName("notas")] string? Notas)
{
    public bool IsApproved => string.Equals(Veredicto, "aprobado", StringComparison.OrdinalIgnoreCase);
}
