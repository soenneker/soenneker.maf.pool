using Soenneker.Maf.Pool.Abstract;
using Soenneker.Asyncs.Locks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Soenneker.Maf.Pool;

internal sealed class SubPool
{
    public readonly ConcurrentDictionary<string, IMafPoolEntry> Entries = new();
    public LinkedList<string> OrderedKeys = [];
    public readonly Dictionary<string, LinkedListNode<string>> NodeMap = new();
    public readonly AsyncLock QueueLock = new();
}
