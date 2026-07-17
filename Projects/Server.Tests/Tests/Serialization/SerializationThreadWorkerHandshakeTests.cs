using System;
using System.Threading;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class SerializationThreadWorkerHandshakeTests
{
    // Regression: the pause handshake cleared _pause and checked the exit flag AFTER
    // signaling _stopEvent. Wake/Sleep/Exit all run on the owning thread, so an Exit()
    // issued the moment a Sleep() returned could either be clobbered (worker spins
    // forever) or orphaned (worker returns without servicing Exit's Sleep) — a silent
    // deadlock. Churn the full lifecycle with the racy back-to-back Sleep/Exit pattern;
    // the watchdog turns a reintroduced deadlock into a failure instead of a hung run.
    [Fact]
    public void WakeSleepExitChurn_NeverDeadlocks()
    {
        Exception failure = null;
        var done = new ManualResetEventSlim();

        var churn = new Thread(() =>
        {
            try
            {
                for (var i = 0; i < 2000; i++)
                {
                    var source = new SerializationChunkSource();
                    var worker = new SerializationThreadWorker(0, source);
                    worker.AllocateHeap();

                    worker.Wake();
                    worker.Sleep();
                    worker.Exit(); // Immediately after Sleep returns — the racy window.
                }
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                done.Set();
            }
        })
        {
            IsBackground = true,
            Name = "Handshake Churn"
        };

        churn.Start();

        Assert.True(
            done.Wait(TimeSpan.FromMinutes(2)),
            "Worker pause/exit handshake deadlocked (owner blocked in Sleep or worker spinning)."
        );
        Assert.Null(failure);
    }
}
