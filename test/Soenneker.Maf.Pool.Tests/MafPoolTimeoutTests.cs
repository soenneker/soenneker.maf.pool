using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Maf.Dtos.Options;

namespace Soenneker.Maf.Pool.Tests;

public sealed class MafPoolTimeoutTests
{
    [Test]
    public async Task ExhaustedQuota_reports_pool_and_remaining_quota()
    {
        var pool = new MafPool(null!);
        await pool.Add("emails", "primary", new MafPoolEntry("primary", new MafOptions { RequestsPerDay = 0 }));

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await pool.GetAvailable("emails", TimeSpan.FromMilliseconds(50))) ?? throw new InvalidOperationException("Expected timeout exception.");

        await Assert.That(exception.Message).Contains("emails");
        await Assert.That(exception.Message).Contains("primary");
        await Assert.That(exception.Message).Contains("day=0");
        await Assert.That(exception.InnerException is OperationCanceledException).IsTrue();
    }

    [Test]
    public async Task EmptyPool_reports_no_registered_entries()
    {
        var pool = new MafPool(null!);
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await pool.GetAvailable("missing", TimeSpan.FromMilliseconds(50))) ?? throw new InvalidOperationException("Expected timeout exception.");

        await Assert.That(exception.Message).Contains("missing");
        await Assert.That(exception.Message).Contains("no registered entries");
    }

    [Test]
    public async Task AlreadyCancelled_caller_throws_instead_of_returning_null()
    {
        var pool = new MafPool(null!);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await pool.GetAvailable("emails", cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await pool.GetAvailable("emails", TimeSpan.FromSeconds(10), cancellation.Token));
    }

    [Test]
    public async Task Caller_cancellation_during_wait_is_not_a_timeout()
    {
        var pool = new MafPool(null!);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        try
        {
            await pool.GetAvailable("emails", Timeout.InfiniteTimeSpan, cancellation.Token);
            throw new InvalidOperationException("Expected caller cancellation.");
        }
        catch (OperationCanceledException)
        {
            await Assert.That(cancellation.IsCancellationRequested).IsTrue();
        }
    }
}
