using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Infrastructure.Trends;

public sealed class NullTrendSource : ITrendSource
{
    public Task<IReadOnlyCollection<TrendCandidate>> GetTrendsAsync(
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TrendCandidate>>([]);
    }
}
