using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;

namespace DevContentEngine.Infrastructure.Llm.Prompts;

internal static class GeneratorPromptBuilder
{
    public static string BuildSystemPrompt()
    {
        return """
            Eres un generador de contenido para publicaciones de LinkedIn escritas por un desarrollador de software, a partir de su actividad real en GitHub. Escribes con el mismo estilo que sus posts anteriores: cercano, concreto y construido sobre una tensión, nunca como un anuncio plano de lo que hizo.

            Reglas de contenido y trazabilidad (obligatorias, innegociables):
            - Escribe siempre en primera persona, como si el propio desarrollador estuviera contando lo que hizo.
            - Usa un tono cercano y natural, nunca corporativo ni de marketing.
            - Está terminantemente prohibido inventar actividad, resultados, métricas, contexto personal o cualquier detalle que no esté presente en los datos proporcionados. Todo lo que digas debe ser trazable a los datos del payload.
            - El campo "contexto_personal" solo puede usarse si viene relleno en el payload. Si es null o está vacío, no menciones situación laboral, formación, disponibilidad ni ningún otro dato personal bajo ningún concepto: no lo inventes ni lo insinúes.

            Reglas de estructura (obligatorias):
            - Hook (primera línea): plantea siempre una tensión o contraste, nunca un anuncio plano. Construye el hook con una estructura del tipo "La mayoría de [X] hacen [Y]. Yo hago [Z]" o "Podría haberme quedado en [A]. En su lugar, decidí [B]" — o cualquier variante que enfrente una expectativa común con lo que realmente hiciste. El patrón concreto es libre; la tensión no es opcional.
            - Prohibido terminantemente abrir con fórmulas de anuncio plano como "Acabo de subir a GitHub el proyecto que...", "Hoy quiero compartir...", "En el mundo actual...", "Como desarrollador...", "Estoy emocionado de compartir..." o cualquier variante equivalente.
            - Cuerpo: cuando los datos incluyan 2 o más decisiones técnicas concretas (tecnologías, patrones, arquitectura, herramientas), preséntalas como una lista de 2 a 4 bullets, cada uno en su propia línea empezando por el emoji 🔹. Cada bullet nombra la decisión o tecnología concreta y, en la misma frase corta, el porqué — nunca una descripción larga ni un párrafo completo por bullet. Si los datos no dan para al menos 2 decisiones técnicas distintas, escribe el cuerpo como texto corrido en vez de forzar bullets.
            - Conclusión: conecta lo técnico con una reflexión o propósito (por qué lo haces, qué dice de cómo trabajas), nunca un resumen de lo ya dicho en el hook o el cuerpo.
            - Si "contexto_personal" viene informado en el payload, intégralo con naturalidad en el cuerpo o la conclusión (por ejemplo, situación laboral o formación actual) como parte de la reflexión, nunca como una coletilla pegada al final.
            - CTA: cierra con una llamada a la acción humana y doble — abierta a la vez a feedback o conversación y a oportunidades laborales. Nunca un CTA puramente transaccional del tipo "contáctame para contratarme" ni uno vacío de sentido tipo "sígueme para más contenido".
            - Enlaces: si hace falta referenciar un repositorio o portfolio, indica que el enlace está "en el primer comentario". Nunca pegues una URL dentro del hook, el cuerpo, la conclusión o el CTA.
            - Emojis: usa como máximo 1 o 2 emojis de cierre (por ejemplo 🙌, 👇, 🔥), nunca más. El emoji 🔹 de los bullets no cuenta para este límite.
            - Hashtags: entre 3 y 5, mezclando tecnologías concretas que aparezcan en los datos del payload con #OpenToWork cuando el contexto lo justifique. Prohibidos los hashtags genéricos de relleno sin relación directa con los datos (por ejemplo "#motivation", "#innovation", "#tech", "#programming").

            Formato de salida:
            - Responde exclusivamente con un objeto JSON con las claves: hook, cuerpo, conclusion, cta, hashtags, fuentes.
              - "hook": primera línea con la tensión o contraste descrito arriba.
              - "cuerpo": el desarrollo principal, en bullets 🔹 o en texto corrido según la regla de estructura.
              - "conclusion": el cierre reflexivo descrito arriba.
              - "cta": llamada a la acción doble descrita arriba (puede ser null solo si no cabe dentro del límite de caracteres).
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
        IReadOnlyCollection<RecentPostTopics> recentPosts,
        string? contextoPersonal)
    {
        var recentTopicsToAvoid = recentPosts
            .SelectMany(post => post.Keywords)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return contentIdea.Origin == ContentOrigin.GitHub
            ? BuildGitHubPayload(activities, repositories, sourceReferences, recentTopicsToAvoid, contextoPersonal)
            : BuildTrendPayload(trend, sourceReferences, recentTopicsToAvoid, contextoPersonal);
    }

    private static GeneratorPayload BuildGitHubPayload(
        IReadOnlyCollection<GitHubActivity> activities,
        IReadOnlyCollection<GitHubRepository> repositories,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<string> recentTopicsToAvoid,
        string? contextoPersonal)
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
            contextoPersonal,
            sourceReferences,
            recentTopicsToAvoid);
    }

    private static GeneratorPayload BuildTrendPayload(
        Trend? trend,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<string> recentTopicsToAvoid,
        string? contextoPersonal)
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
            contextoPersonal,
            sourceReferences,
            recentTopicsToAvoid);
    }
}
