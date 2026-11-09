using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class WorldSaveFailureTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"muo-save-failure-{Guid.NewGuid():N}");
    private readonly string _previousSavePath;
    private readonly EventLoopContext _previousLoopContext;

    public WorldSaveFailureTests()
    {
        Directory.CreateDirectory(_root);
        _previousSavePath = World.SavePath;
        _previousLoopContext = Core.LoopContext;

        // Owned by this thread, so the test can run what the save posts back to the loop.
        Core.LoopContext = new EventLoopContext();
    }

    public void Dispose()
    {
        Core.LoopContext = _previousLoopContext;
        World.SetSavePathForTest(_previousSavePath);

        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public void FailedPreparation_ReleasesTheWriteHandle_AndReturnsTheWorldToRunning()
    {
        // A file where Saves/ belongs makes the staged-save recovery throw before anything is frozen.
        var savePath = Path.Combine(_root, "Saves");
        File.WriteAllText(savePath, "not a directory");
        World.SetSavePathForTest(savePath);
        Directory.CreateDirectory(World.StagedSavePath);

        Assert.Equal(WorldState.Running, World.WorldState);

        World.Save();

        // Shutdown waits on this handle.
        Assert.True(World.WaitForWriteCompletion(Timeout));

        var watch = Stopwatch.StartNew();
        while (World.WorldState != WorldState.Running && watch.Elapsed < Timeout)
        {
            Core.LoopContext.ExecuteTasks();
            Thread.Sleep(10);
        }

        Assert.Equal(WorldState.Running, World.WorldState);
    }
}
