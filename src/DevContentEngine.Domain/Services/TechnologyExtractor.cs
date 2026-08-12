namespace DevContentEngine.Domain.Services;

public sealed class TechnologyExtractor
{
    private readonly IReadOnlyCollection<TechnologyMapping> _mappings;

    public TechnologyExtractor(IEnumerable<TechnologyMapping>? mappings = null)
    {
        _mappings = (mappings ?? DefaultTechnologyMappings.Mappings).ToList();
    }

    public IReadOnlyCollection<string> Extract(IEnumerable<string>? filePaths, IEnumerable<string>? commitMessages)
    {
        var paths = (filePaths ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
        var messages = (commitMessages ?? []).Where(message => !string.IsNullOrWhiteSpace(message)).ToList();

        var detected = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in _mappings)
        {
            if (MatchesAnyExtension(mapping, paths) ||
                MatchesAnyPathKeyword(mapping, paths) ||
                MatchesAnyMessageKeyword(mapping, messages))
            {
                detected.Add(mapping.Technology);
            }
        }

        return detected.ToList();
    }

    private static bool MatchesAnyExtension(TechnologyMapping mapping, IReadOnlyCollection<string> paths)
    {
        if (mapping.FileExtensions.Count == 0)
            return false;

        return paths.Any(path => mapping.FileExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesAnyPathKeyword(TechnologyMapping mapping, IReadOnlyCollection<string> paths)
    {
        if (mapping.PathKeywords.Count == 0)
            return false;

        return paths.Any(path => mapping.PathKeywords.Any(keyword => path.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesAnyMessageKeyword(TechnologyMapping mapping, IReadOnlyCollection<string> messages)
    {
        if (mapping.MessageKeywords.Count == 0)
            return false;

        return messages.Any(message => mapping.MessageKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }
}
