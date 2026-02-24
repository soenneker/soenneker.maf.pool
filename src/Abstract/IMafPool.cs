using Microsoft.Agents.AI;
using Soenneker.Maf.Dtos.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Maf.Pool.Abstract;

/// <summary>
/// Defines a pool of Microsoft Agent Framework <see cref="AIAgent"/> entries, organized by poolId.
/// Allows registering, unregistering, clearing, and checking out an agent instance.
/// </summary>
public interface IMafPool
{
    /// <summary>
    /// Attempts to fetch an available <see cref="AIAgent"/> from the specified pool.
    /// Will retry every 500ms until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a tuple of:
    /// - <see cref="AIAgent"/> instance (or null if cancelled)
    /// - Corresponding <see cref="IMafPoolEntry"/> used to manage that agent.
    /// </returns>
    ValueTask<(AIAgent? agent, IMafPoolEntry? entry)> GetAvailable(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the remaining usage quotas for every entry in the specified pool.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a <see cref="Dictionary{TKey, TValue}"/>,
    /// where each key is an entryKey and the value is a tuple of
    /// (secondsRemaining, minutesRemaining, daysRemaining).
    /// </returns>
    ValueTask<Dictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new agent entry under the specified poolId using <paramref name="options"/>.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this agent entry.</param>
    /// <param name="options"><see cref="MafOptions"/> must have <see cref="MafOptions.AgentFactory"/> set.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask Add(string poolId, string entryKey, MafOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing <see cref="IMafPoolEntry"/> under the specified poolId.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this agent entry.</param>
    /// <param name="entry">Pre-constructed <see cref="IMafPoolEntry"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask Add(string poolId, string entryKey, IMafPoolEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters (removes) the entry with <paramref name="entryKey"/> from the specified pool.
    /// Also removes that entry from the internal cache.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Key of the agent entry to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the entry existed and was removed; false if it was not present.</returns>
    ValueTask<bool> Remove(string poolId, string entryKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears and removes all entries from the specified poolId, and also clears the internal cache.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask Clear(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears and removes every sub-pool (all poolIds) and clears the internal cache completely.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask ClearAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to fetch the <see cref="IMafPoolEntry"/> for a given poolId and entryKey without modifying state.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Key of the agent entry to look up.</param>
    /// <param name="entry">When this method returns, contains the <see cref="IMafPoolEntry"/> if found; otherwise null.</param>
    /// <returns>True if the entry was found; otherwise false.</returns>
    bool TryGet(string poolId, string entryKey, out IMafPoolEntry? entry);
}
