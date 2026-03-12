using Soenneker.Maf.Pool.Abstract;
using Soenneker.Maf.Dtos.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Maf.Pool;

/// <inheritdoc cref="IMafPoolEntry"/>
public sealed class MafPoolEntry : IMafPoolEntry
{
    public IMafRateLimiter RateLimiter { get; }

    public MafOptions Options { get; }

    public string Key { get; }

    public MafPoolEntry(string key, MafOptions options)
    {
        Key = key;
        Options = options;
        RateLimiter = new MafRateLimiter(options);
    }

    public ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default)
    {
        return RateLimiter.TryConsume(cancellationToken);
    }

    public ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default)
    {
        return RateLimiter.GetRemaining(cancellationToken);
    }
}
