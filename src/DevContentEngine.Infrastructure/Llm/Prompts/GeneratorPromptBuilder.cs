using DevContentEngine.Application.Interfaces.External.Models;
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
            - Responde exclusivamente con un objeto JSON con las claves: hook, cuerpo, conclusion, cta, hashtags, fuentes, diagrama.
              - "hook": primera línea con la tensión o contraste descrito arriba.
              - "cuerpo": el desarrollo principal, en bullets 🔹 o en texto corrido según la regla de estructura.
              - "conclusion": el cierre reflexivo descrito arriba.
              - "cta": llamada a la acción doble descrita arriba (puede ser null solo si no cabe dentro del límite de caracteres).
              - "hashtags": lista de 3 a 5 hashtags relevantes.
              - "fuentes": lista de referencias (URLs o identificadores) a los datos reales usados; nunca vacía.
              - "diagrama": una descripción breve EN INGLÉS (2 a 4 frases) de qué debería mostrar visualmente un diagrama de arquitectura que acompañe a este post, basada ÚNICAMENTE en las tecnologías, componentes y flujo mencionados en "cuerpo" y "conclusion". Nunca inventes componentes, servicios ni tecnologías que no aparezcan en el texto real del post. Nunca vacío.
            - Límite estricto e innegociable: la longitud total del texto (hook + cuerpo + conclusion + cta, sumando caracteres) NUNCA puede superar los 1400 caracteres ni bajar de 900. Un borrador que se pase de 1400 caracteres será rechazado automáticamente, sin excepción.
            - Si la actividad incluye muchos commits, issues o pull requests, NO los listes todos: elige los 2-3 más relevantes o representativos y resume el resto en una sola frase. Prioriza siempre quedarte corto de caracteres antes que exceder el límite.
            - Antes de responder, cuenta mentalmente los caracteres de hook + cuerpo + conclusion + cta y recorta el texto si se acerca a 1400.
            """;
    }

    public static string BuildRepoHighlightSystemPrompt()
    {
        return """
            Eres un generador de contenido para publicaciones de LinkedIn escritas por un desarrollador de software, a partir de UNO de sus repositorios ya existentes. A diferencia de un post de actividad reciente, aquí no hay commits del día: el objetivo es destacar una característica concreta y ya implementada del proyecto, en su estado actual. Escribes con el mismo estilo que sus posts anteriores: cercano, concreto y construido sobre una tensión, nunca como un anuncio plano.

            Reglas de contenido y trazabilidad (obligatorias, innegociables):
            - Escribe siempre en primera persona, como si el propio desarrollador estuviera contando su proyecto.
            - Usa un tono cercano y natural, nunca corporativo ni de marketing.
            - Elige UNA sola característica concreta y ya implementada del repositorio (no un resumen de todo el proyecto). Básala únicamente en lo que aparece en "descripcion", "readme", "tecnologías" o "contexto_adicional" del payload.
            - Está terminantemente prohibido hablar de roadmap, planes futuros, "próximamente" o cualquier funcionalidad que no esté ya implementada según el material proporcionado.
            - Está terminantemente prohibido inventar métricas, resultados, usuarios, cifras de adopción o cualquier detalle que no esté presente en los datos proporcionados. Todo lo que digas debe ser trazable al payload.
            - El campo "contexto_personal" solo puede usarse si viene relleno en el payload. Si es null o está vacío, no menciones situación laboral, formación, disponibilidad ni ningún otro dato personal bajo ningún concepto.
            - Si "temas_recientes_a_evitar" incluye ángulos ya usados recientemente para este mismo repositorio, elige una característica o enfoque distinto; no repitas el mismo ángulo.

            Reglas de estructura (obligatorias, iguales a las de un post de actividad reciente):
            - Hook (primera línea): plantea siempre una tensión o contraste, nunca un anuncio plano. Construye el hook con una estructura del tipo "La mayoría de [X] hacen [Y]. Yo hago [Z]" o "Podría haberme quedado en [A]. En su lugar, decidí [B]" — o cualquier variante que enfrente una expectativa común con lo que realmente hiciste en el proyecto. El patrón concreto es libre; la tensión no es opcional.
            - Prohibido terminantemente abrir con fórmulas de anuncio plano como "Acabo de subir a GitHub el proyecto que...", "Hoy quiero compartir...", "En el mundo actual...", "Como desarrollador...", "Estoy emocionado de compartir..." o cualquier variante equivalente.
            - Cuerpo: si la característica elegida involucra 2 o más decisiones técnicas concretas (tecnologías, patrones, arquitectura, herramientas), preséntalas como una lista de 2 a 4 bullets, cada uno en su propia línea empezando por el emoji 🔹. Cada bullet nombra la decisión o tecnología concreta y, en la misma frase corta, el porqué. Si solo hay una decisión clara, escribe el cuerpo como texto corrido en vez de forzar bullets.
            - Conclusión: conecta la característica técnica con una reflexión o propósito (qué dice esa decisión sobre cómo diseñas o piensas el proyecto), nunca un resumen de lo ya dicho.
            - Si "contexto_personal" viene informado en el payload, intégralo con naturalidad en el cuerpo o la conclusión, nunca como una coletilla pegada al final.
            - CTA: cierra con una llamada a la acción humana y doble — abierta a la vez a feedback o conversación y a oportunidades laborales. Nunca un CTA puramente transaccional ni uno vacío de sentido tipo "sígueme para más contenido".
            - Enlaces: indica que el enlace al repositorio está "en el primer comentario". Nunca pegues una URL dentro del hook, el cuerpo, la conclusión o el CTA.
            - Emojis: usa como máximo 1 o 2 emojis de cierre (por ejemplo 🙌, 👇, 🔥), nunca más. El emoji 🔹 de los bullets no cuenta para este límite.
            - Hashtags: entre 3 y 5, mezclando tecnologías concretas que aparezcan en los datos del payload con #OpenToWork cuando el contexto lo justifique. Prohibidos los hashtags genéricos de relleno sin relación directa con los datos.

            Formato de salida:
            - Responde exclusivamente con un objeto JSON con las claves: hook, cuerpo, conclusion, cta, hashtags, fuentes, diagrama.
              - "hook": primera línea con la tensión o contraste descrito arriba.
              - "cuerpo": el desarrollo principal, en bullets 🔹 o en texto corrido según la regla de estructura.
              - "conclusion": el cierre reflexivo descrito arriba.
              - "cta": llamada a la acción doble descrita arriba (puede ser null solo si no cabe dentro del límite de caracteres).
              - "hashtags": lista de 3 a 5 hashtags relevantes.
              - "fuentes": lista de referencias a los datos reales usados; nunca vacía. Debe incluir SIEMPRE, sin excepción, cada URL presente en "enlaces" tal cual aparece ahí — no la parafrasees ni la sustituyas por una descripción del contenido. Puedes añadir además otras referencias (por ejemplo, qué sección del README o qué campo del payload usaste), pero la URL de "enlaces" es obligatoria en "fuentes".
              - "diagrama": una descripción breve EN INGLÉS (2 a 4 frases) de qué debería mostrar visualmente un diagrama de arquitectura que acompañe a este post, basada ÚNICAMENTE en el repositorio, tecnologías, README o estructura de carpetas del payload y en lo que digas en "cuerpo". Nunca inventes componentes, servicios ni tecnologías que no aparezcan en el payload o en el texto real del post. Nunca vacío.
            - Límite estricto e innegociable: la longitud total del texto (hook + cuerpo + conclusion + cta, sumando caracteres) NUNCA puede superar los 1400 caracteres ni bajar de 900. Un borrador que se pase de 1400 caracteres será rechazado automáticamente, sin excepción.
            - Antes de responder, cuenta mentalmente los caracteres de hook + cuerpo + conclusion + cta y recorta el texto si se acerca a 1400.
            """;
    }

    public static GeneratorPayload BuildPayload(
        ContentIdea contentIdea,
        IReadOnlyCollection<GitHubActivity> activities,
        IReadOnlyCollection<GitHubRepository> repositories,
        Trend? trend,
        GitHubRepositoryDetail? repositoryDetail,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<RecentPostTopics> recentPosts,
        string? contextoPersonal)
    {
        var recentTopicsToAvoid = recentPosts
            .SelectMany(post => post.Keywords)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return contentIdea.Origin switch
        {
            ContentOrigin.GitHub => BuildGitHubPayload(activities, repositories, sourceReferences, recentTopicsToAvoid, contextoPersonal),
            ContentOrigin.Trend => BuildTrendPayload(trend, sourceReferences, recentTopicsToAvoid, contextoPersonal),
            ContentOrigin.RepoHighlight => BuildRepoHighlightPayload(
                repositories, repositoryDetail, sourceReferences, recentTopicsToAvoid, contextoPersonal),
            _ => throw new ArgumentOutOfRangeException(nameof(contentIdea), contentIdea.Origin, "Unknown content origin.")
        };
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
            null,
            null,
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
            null,
            null,
            sourceReferences,
            recentTopicsToAvoid);
    }

    private static GeneratorPayload BuildRepoHighlightPayload(
        IReadOnlyCollection<GitHubRepository> repositories,
        GitHubRepositoryDetail? repositoryDetail,
        IReadOnlyCollection<string> sourceReferences,
        IReadOnlyCollection<string> recentTopicsToAvoid,
        string? contextoPersonal)
    {
        var proyecto = repositories.Count > 0
            ? string.Join(", ", repositories.Select(repository => repository.FullName).Distinct())
            : null;

        var tecnologias = new List<string>();

        if (!string.IsNullOrWhiteSpace(repositoryDetail?.PrimaryLanguage))
        {
            tecnologias.Add(repositoryDetail!.PrimaryLanguage!);
        }

        if (repositoryDetail is not null)
        {
            tecnologias.AddRange(repositoryDetail.Topics);
        }

        var contextoAdicional = repositoryDetail is null || repositoryDetail.TopLevelFolders.Count == 0
            ? null
            : $"Estructura de carpetas de alto nivel: {string.Join(", ", repositoryDetail.TopLevelFolders)}";

        return new GeneratorPayload(
            "RepoHighlight",
            proyecto,
            tecnologias,
            [],
            null,
            null,
            contextoAdicional,
            contextoPersonal,
            repositoryDetail?.Description,
            repositoryDetail?.ReadmeExcerpt,
            sourceReferences,
            recentTopicsToAvoid);
    }
}
