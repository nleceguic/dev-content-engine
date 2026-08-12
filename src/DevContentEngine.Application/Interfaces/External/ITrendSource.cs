using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Application.Interfaces.External;

public interface ITrendSource
{
    Task<IReadOnlyCollection<TrendCandidate>> GetTrendsAsync(
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken = default);
}
