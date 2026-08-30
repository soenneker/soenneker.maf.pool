[![](https://img.shields.io/nuget/v/soenneker.maf.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.maf.pool/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.maf.pool/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.maf.pool/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.maf.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.maf.pool/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.maf.pool/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.maf.pool/actions/workflows/codeql.yml)

# Soenneker.Maf.Pool

Selects an available Microsoft Agent Framework `AIAgent` from named pools while enforcing per-entry request quotas.

## Install

```bash
dotnet add package Soenneker.Maf.Pool
```

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Maf.Dtos.Options;
using Soenneker.Maf.Pool.Abstract;
using Soenneker.Maf.Pool.Registrars;

services.AddMafPoolAsSingleton();

IMafPool pool = serviceProvider.GetRequiredService<IMafPool>();

await pool.Add("chat", "primary", new MafOptions
{
    ModelId = "my-model",
    RequestsPerMinute = 60,
    AgentFactory = static (options, cancellationToken) =>
        CreateAgentAsync(options.ModelId!, cancellationToken)
}, cancellationToken);

(AIAgent? agent, IMafPoolEntry? entry) =
    await pool.GetAvailable("chat", cancellationToken);

if (agent is not null)
{
    // Use the selected agent. The request allowance was consumed by checkout.
}
```

Add multiple entries to a pool to provide fallback capacity. Entries are checked in registration order; an entry whose quota is exhausted is skipped until capacity becomes available.

## What you get

- `IMafPool` — Defines a pool of Microsoft Agent Framework `AIAgent` entries, organized by poolId. Allows registering, unregistering, clearing, and checking out an agent instance.
- `IMafPoolEntry` — Represents a single agent source (model + config) with rate limiting capabilities.
- `IMafRateLimiter` — Represents a rate limiter that tracks requests per second, per minute, and per day using sliding windows.
- `MafPoolRegistrar` — Registration extensions for `IMafCache` and `IMafPool`.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IMafPool.GetAvailable(poolId, cancellationToken)` | Attempts to fetch an available `AIAgent` from the specified pool. Will retry every 500ms until `cancellationToken` is cancelled. | A `ValueTask{TResult}` containing a tuple of: - `AIAgent` instance (or null if cancelled) - Corresponding `IMafPoolEntry` used to manage that agent. |
| `IMafPool.GetRemainingQuotas(poolId, cancellationToken)` | Retrieves the remaining usage quotas for every entry in the specified pool. | A `ValueTask{TResult}` containing a `Dictionary{TKey, TValue}`, where each key is an entryKey and the value is a tuple of (secondsRemaining, minutesRemaining, daysRemaining). |
| `IMafPool.Add(poolId, entryKey, options, cancellationToken)` | Registers a new agent entry under the specified poolId using `options`. | A task that completes when the add operation is complete. |
| `IMafPool.Add(poolId, entryKey, entry, cancellationToken)` | Registers an existing `IMafPoolEntry` under the specified poolId. | A task that completes when the add operation is complete. |
| `IMafPool.Remove(poolId, entryKey, cancellationToken)` | Unregisters (removes) the entry with `entryKey` from the specified pool. Also removes that entry from the internal cache. | True if the entry existed and was removed; false if it was not present. |
| `IMafPool.Clear(poolId, cancellationToken)` | Clears and removes all entries from the specified poolId, and also clears the internal cache. | A task that completes when the Maf Pool has been cleared. |
| `IMafPool.ClearAll(cancellationToken)` | Clears and removes every sub-pool (all poolIds) and clears the internal cache completely. | A task that completes when the Maf Pool has been cleared. |
| `IMafPool.TryGet(poolId, entryKey, entry)` | Attempts to fetch the `IMafPoolEntry` for a given poolId and entryKey without modifying state. | True if the entry was found; otherwise false. |
| `IMafPoolEntry.RateLimiter` | Gets rate limiter. | Gets rate limiter. |
| `IMafPoolEntry.Options` | Gets options. | Gets options. |
| `IMafPoolEntry.Key` | Gets key. | Gets key. |
| `IMafPoolEntry.IsAvailable(cancellationToken)` | Gets whether this agent is currently available based on rate limits. | true if gets whether this agent is currently available based on rate limits; otherwise, false. |
| `IMafPoolEntry.RemainingQuota(cancellationToken)` | Gets the remaining quota for this pool entry. | A task whose result is the requested (int Second, int Minute, int Day). |
| `IMafRateLimiter.TryConsume(cancellationToken)` | Attempts to consume a token from the rate limiter. | True if a token was consumed successfully, false if the rate limit was exceeded. |
| `IMafRateLimiter.GetRemaining(cancellationToken)` | Gets the remaining quota for each time window. | A tuple containing the remaining requests for second, minute, and day windows. |
| `MafPoolRegistrar.AddMafPoolAsSingleton(services)` | Adds `IMafCache` and `IMafPool` as singleton services. | The same service collection, so additional registrations can be chained. |
| `MafPoolRegistrar.AddMafPoolAsScoped(services)` | Adds `IMafCache` and `IMafPool` as scoped services. | The same service collection, so additional registrations can be chained. |

## Practical notes

- `GetAvailable()` waits when every entry is rate-limited. Always pass a cancellation token when an indefinite wait is undesirable.
- A successful checkout consumes one request from the selected entry. The pool does not observe whether the eventual model request succeeds.
- Pool ids isolate both registrations and cached agents, so the same entry key can be used in different pools.
- `Clear(poolId)` affects only that pool. `ClearAll()` removes every pool and cached agent.
- Singleton registration shares quotas and agents application-wide; scoped registration creates independent pools per scope.
