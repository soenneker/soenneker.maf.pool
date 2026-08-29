using Soenneker.Maf.Dtos.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Maf.Pool.Abstract;

/// <summary>
/// Represents a single agent source (model + config) with rate limiting capabilities.
/// </summary>
public interface IMafPoolEntry
{
    /// <summary>
    /// Gets rate limiter.
    /// </summary>
    IMafRateLimiter RateLimiter { get; }

    /// <summary>
    /// Gets options.
    /// </summary>
    MafOptions Options { get; }

    /// <summary>
    /// Gets key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets whether this agent is currently available based on rate limits.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if gets whether this agent is currently available based on rate limits; otherwise, false.</returns>
    ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for this pool entry.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested (int Second, int Minute, int Day).</returns>
    ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default);
}
