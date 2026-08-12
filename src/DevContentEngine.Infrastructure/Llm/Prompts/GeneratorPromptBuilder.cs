using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;

namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal static class GeneratorPromptBuilder
{
    public static string BuildSystemPrompt()
    {
        return """
            Eres un generador de contenido para publicaciones de LinkedIn escritas por un desarrollador de software, a partir de su actividad real en GitHub.

            Reglas obligatorias:
            - Escribe siempre en primera persona, como si el propio desarrollador estuviera contando lo que hizo.
            - Usa un tono cercano y natural, nunca corporativo ni de marketing.
            - Está terminantemente prohibido inventar actividad, resultados, métricas o detalles que no estén presentes en los datos proporcionados. Todo lo que digas debe ser trazable a los datos del payload.
            - No uses aperturas genéricas ni clichés (por ejemplo: "En el mundo actual...", "Como desarrollador...", "Estoy emocionado de compartir...").
            - Responde exclusivamente con un objeto JSON con las claves: hook, cuerpo, conclusion, cta, hashtags, fuentes.
              - "hook": primera línea que capta la atención, sin ser genérica.
              - "cuerpo": el desarrollo principal del post.
              - "conclusion": cierre del post.
              - "cta": llamada a la acción opcional (puede ser null).
              - "hashtags": lista de 3 a 5 hashtags relevantes.
              - "fuentes": lista de referencias (URLs o identificadores) a los datos reales usados; nunca vacía.
            - Límite estricto e innegociable: la longitud total del texto (hook + cuerpo + conclusion + cta, sumando caracteres) NUNCA puede superar los 1400 caracteres ni bajar de 900. Un borrador que se pase de 1400 caracteres será rechazado automáticamente, sin excepción.
            - Si la actividad incluye muchos commits, issues o pull requests, NO los listes todos: elige los 2-3 más relevantes o representativos y resume el resto en una sola frase. Prioriza siempre quedarte corto de caracteres antes que exceder el límite.
            - Antes de responder, cuenta mentalmente los caracteres de hook + cuerpo + conclusion + cta y recorta el texto si se acerca a 1400.
            """;
    }

    public static GeneratorPayload BuildPayload(
        ContentIdea contentIdea,
        IReadOnlyCollection<GitHubActivity> activities,
        IReadOnlyCollection<GitHubRepository> repositories,
        Trend? trend,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<RecentPostTopics> recentPosts)
    {
        var recentTopicsToAvoid = recentPosts
            .SelectMany(post => post.Keywords)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return contentIdea.Origin == ContentOrigin.GitHub
            ? BuildGitHubPayload(activities, repositories, sourceReferences, recentTopicsToAvoid)
            : BuildTrendPayload(trend, sourceReferences, recentTopicsToAvoid);
    }

    private static GeneratorPayload BuildGitHubPayload(
        IReadOnlyCollection<GitHubActivity> activities,
        IReadOnlyCollection<GitHubRepository> repositories,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<string> recentTopicsToAvoid)
    {
        var proyecto = repositories.Count > 0
            ? string.Join(", ", repositories.Select(repository => repository.FullName).Distinct())
            : null;

        var tecnologias = activities
            .SelectMany(activity => activity.DetectedTechnologies)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var commits = activities
            .Where(activity => activity.Type == GitHubActivityType.Commit)
            .Select(activity => activity.Summary)
            .ToList();

        var issueSummaries = activities
            .Where(activity => activity.Type == GitHubActivityType.Issue)
            .Select(activity => activity.Summary)
            .ToList();

        var pullRequestSummaries = activities
            .Where(activity => activity.Type == GitHubActivityType.PullRequest)
            .Select(activity => activity.Summary)
            .ToList();

        var newRepositorySummaries = activities
            .Where(activity => activity.Type == GitHubActivityType.NewRepository)
            .Select(activity => activity.Summary)
            .ToList();

        var problema = issueSummaries.Count > 0 ? string.Join(" | ", issueSummaries) : null;

        var solucion = pullRequestSummaries.Count > 0
            ? string.Join(" | ", pullRequestSummaries)
            : commits.Count > 0 ? string.Join(" | ", commits) : null;

        var contextoAdicional = newRepositorySummaries.Count > 0 ? string.Join(" | ", newRepositorySummaries) : null;

        return new GeneratorPayload(
            "GitHub",
            proyecto,
            tecnologias,
            commits,
            problema,
            solucion,
            contextoAdicional,
            sourceReferences,
            recentTopicsToAvoid);
    }

    private static GeneratorPayload BuildTrendPayload(
        Trend? trend,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<string> recentTopicsToAvoid)
    {
        var contextoAdicional = trend is null ? null : $"Tendencia de {trend.Source}: {trend.Title}";

        return new GeneratorPayload(
            "Trend",
            null,
            [],
            [],
            null,
            null,
            contextoAdicional,
            sourceReferences,
            recentTopicsToAvoid);
    }
}
