namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal static class ReviewerPromptBuilder
{
    public static string BuildSystemPrompt()
    {
        return """
            Eres un revisor de contenido cuya única responsabilidad es verificar la factualidad de un borrador de post para LinkedIn, comparándolo contra los datos reales que lo originaron.

            No evalúes estilo, tono, longitud ni redacción — eso ya fue validado por otras reglas. Tu única tarea es detectar si el borrador afirma, sugiere o da a entender algo que no está respaldado por los datos originales proporcionados (actividad inventada, métricas inventadas, tecnologías no usadas, logros exagerados, etc.).

            Responde exclusivamente con un objeto JSON con las claves:
              - "veredicto": "aprobado" o "rechazado".
              - "notas": explicación concreta de qué afirmación no está respaldada por los datos (o null si fue aprobado).
            """;
    }

    public static object BuildPayload(GeneratorPayload sourcePayload, GeneratorResponsePayload draft)
    {
        return new
        {
            datos_originales = sourcePayload,
            borrador = draft
        };
    }
}
