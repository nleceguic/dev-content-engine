using System.Text.Json.Serialization;

namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal sealed record GeneratorPayload(
    [property: JsonPropertyName("origen")] string Origen,
    [property: JsonPropertyName("proyecto")] string? Proyecto,
    [property: JsonPropertyName("tecnologías")] IReadOnlyCollection<string> Tecnologias,
    [property: JsonPropertyName("commits")] IReadOnlyCollection<string> Commits,
    [property: JsonPropertyName("problema")] string? Problema,
    [property: JsonPropertyName("solución")] string? Solucion,
    [property: JsonPropertyName("contexto_adicional")] string? ContextoAdicional,
    [property: JsonPropertyName("enlaces")] IReadOnlyCollection<string> Enlaces,
    [property: JsonPropertyName("temas_recientes_a_evitar")] IReadOnlyCollection<string> TemasRecientesAEvitar);
