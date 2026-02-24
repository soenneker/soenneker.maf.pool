using Soenneker.Maf.Dtos.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Maf.Pool.Abstract;

/// <summary>
/// Represents a single agent source (model + config) with rate limiting capabilities.
/// </summary>
public interface IMafPoolEntry
{
    IMafRateLimiter RateLimiter { get; }

    MafOptions Options { get; }

    string Key { get; }

    /// <summary>
    /// Gets whether this agent is currently available based on rate limits.
    /// </summary>
    ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for this pool entry.
    /// </summary>
    ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default);
}
