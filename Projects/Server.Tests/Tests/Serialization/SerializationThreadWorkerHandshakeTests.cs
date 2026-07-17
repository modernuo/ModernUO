using System;
using System.Threading;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class SerializationThreadWorkerHandshakeTests
{
    // The pause handshake must tolerate a new cycle starting the moment _stopEvent is
    // set (Exit right after Sleep). The watchdog turns a reintroduced deadlock into a
    // failure instead of a hung run.
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
