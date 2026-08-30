using Microsoft.Agents.AI;
using Soenneker.Extensions.ValueTask;
using Soenneker.Maf.Cache.Abstract;
using Soenneker.Maf.Dtos.Options;
using Soenneker.Maf.Pool.Abstract;
using Soenneker.Utils.Delay;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Maf.Pool;

public sealed class MafPool : IMafPool
{
    private readonly ConcurrentDictionary<string, SubPool> _subPools = new();
    private readonly IMafCache _cache;

    public MafPool(IMafCache cache)
    {
        _cache = cache;
    }

    public async ValueTask<(AIAgent? agent, IMafPoolEntry? entry)> GetAvailable(string poolId, CancellationToken cancellationToken = default)
    {
        SubPool pool = _subPools.GetOrAdd(poolId, _ => new SubPool());

        while (!cancellationToken.IsCancellationRequested)
        {
            List<(string Key, IMafPoolEntry Entry)> candidates;

            using (await pool.QueueLock.Lock(cancellationToken).NoSync())
            {
                candidates = new List<(string, IMafPoolEntry)>(pool.Entries.Count);

                foreach (string key in pool.OrderedKeys)
                {
                    if (!pool.Entries.TryGetValue(key, out IMafPoolEntry? entry))
                        continue;
                    candidates.Add((key, entry));
                }
            }

            foreach ((string key, IMafPoolEntry entry) in candidates)
            {
                if (!await entry.IsAvailable(cancellationToken).NoSync())
                    continue;

                if (!pool.Entries.TryGetValue(key, out IMafPoolEntry? stillLive) || !ReferenceEquals(stillLive, entry))
                    continue;

                AIAgent agent = await _cache.Get(GetCacheKey(poolId, key), entry.Options, cancellationToken).NoSync();
                return (agent, entry);
            }

            await DelayUtil.Delay(TimeSpan.FromMilliseconds(500), null, cancellationToken).NoSync();
        }

        return (null, null);
    }

    public async ValueTask<Dictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(string poolId, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, (int, int, int)>();

        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return result;

        foreach (KeyValuePair<string, IMafPoolEntry> kvp in pool.Entries)
        {
            (int Second, int Minute, int Day) quota = await kvp.Value.RemainingQuota(cancellationToken).NoSync();
            result[kvp.Key] = quota;
        }

        return result;
    }

    public async ValueTask Add(string poolId, string entryKey, MafOptions options, CancellationToken cancellationToken = default)
    {
        if (options.AgentFactory == null)
            throw new ArgumentException("AgentFactory must be set on MafOptions", nameof(options));

        var entry = new MafPoolEntry(entryKey, options);
        await Add(poolId, entryKey, entry, cancellationToken);
    }

    public async ValueTask Add(string poolId, string entryKey, IMafPoolEntry entry, CancellationToken cancellationToken = default)
    {
        SubPool pool = _subPools.GetOrAdd(poolId, _ => new SubPool());

        using (await pool.QueueLock.Lock(cancellationToken).NoSync())
        {
            if (pool.Entries.TryAdd(entryKey, entry))
            {
                LinkedListNode<string> node = pool.OrderedKeys.AddLast(entryKey);
                pool.NodeMap[entryKey] = node;
            }
        }
    }

    public async ValueTask<bool> Remove(string poolId, string entryKey, CancellationToken cancellationToken = default)
    {
        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return false;

        using (await pool.QueueLock.Lock(cancellationToken).NoSync())
        {
            if (!pool.Entries.TryRemove(entryKey, out _))
                return false;

            if (pool.NodeMap.TryGetValue(entryKey, out LinkedListNode<string>? node))
            {
                pool.OrderedKeys.Remove(node);
                pool.NodeMap.Remove(entryKey);
            }
        }

        await _cache.Remove(GetCacheKey(poolId, entryKey), cancellationToken).NoSync();
        return true;
    }

    public async ValueTask Clear(string poolId, CancellationToken cancellationToken = default)
    {
        if (!_subPools.TryRemove(poolId, out SubPool? pool))
            return;

        List<string> keys;
        using (await pool.QueueLock.Lock(cancellationToken).NoSync())
        {
            keys = [.. pool.Entries.Keys];
            pool.Entries.Clear();
            pool.OrderedKeys = [];
            pool.NodeMap.Clear();
        }

        foreach (string entryKey in keys)
            await _cache.Remove(GetCacheKey(poolId, entryKey), cancellationToken).NoSync();
    }

    public async ValueTask ClearAll(CancellationToken cancellationToken = default)
    {
        _subPools.Clear();
        await _cache.Clear(cancellationToken).NoSync();
    }

    public bool TryGet(string poolId, string entryKey, out IMafPoolEntry? entry)
    {
        entry = null;

        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return false;

        return pool.Entries.TryGetValue(entryKey, out entry);
    }

    private static string GetCacheKey(string poolId, string entryKey)
    {
        return $"{poolId.Length}:{poolId}{entryKey}";
    }
}
