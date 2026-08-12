namespace DevContentEngine.Domain.Services;

public static class ImagePromptGenerator
{
    public static string Build(string title, string subtitle, string diagramDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(subtitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagramDescription);

        var normalizedDescription = NormalizeDescription(diagramDescription);

        return
            "Dark navy/indigo tech background with a subtle dot-grid texture. " +
            "Minimalist glowing architecture-diagram style illustration, electric blue-to-violet neon " +
            "glow on rounded rectangle nodes connected by thin directional arrows, clean modern " +
            "infographic aesthetic, not photorealistic. Small circular logo badge top-left. " +
            $"Bold white title reading '{title}'. Small light-gray subtitle reading '{subtitle}'. " +
            $"The diagram should visually represent: {normalizedDescription} " +
            "High contrast, professional software-architecture-diagram style, suitable as a LinkedIn " +
            "post cover image, 1200x630.";
    }

    private static string NormalizeDescription(string diagramDescription)
    {
        var trimmed = diagramDescription.Trim();

        return trimmed.EndsWith('.') ? trimmed : trimmed + ".";
    }
}
