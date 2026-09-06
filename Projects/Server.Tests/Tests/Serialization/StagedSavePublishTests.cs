using System;
using System.IO;
using Xunit;

namespace Server.Tests;

/// <summary>
/// The publish protocol: a complete snapshot is staged next to Saves/ before the previous
/// save is touched, and an interrupted publish is finished at the next boot or save.
/// </summary>
[Collection("Sequential Server Tests")]
public class StagedSavePublishTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"muo-staged-{Guid.NewGuid():N}");
    private readonly string _previousSavePath;

    public StagedSavePublishTests()
    {
        Directory.CreateDirectory(_root);
        _previousSavePath = World.SavePath;
        World.SetSavePathForTest(Path.Combine(_root, "Saves"));
    }

    public void Dispose()
    {
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

    private static void WriteMarker(string dir, string name)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "marker.txt"), name);
    }

    private static string ReadMarker(string dir) => File.ReadAllText(Path.Combine(dir, "marker.txt"));

    [Fact]
    public void NothingStaged_RecoveryIsANoOp()
    {
        WriteMarker(World.SavePath, "current");

        World.RecoverStagedSave();

        Assert.Equal("current", ReadMarker(World.SavePath));
        Assert.Single(Directory.GetDirectories(_root));
    }

    [Fact]
    public void StagedSave_ReplacesSaves_AndKeepsThePreviousOne()
    {
        WriteMarker(World.SavePath, "old");
        WriteMarker(World.StagedSavePath, "new");

        World.RecoverStagedSave();

        Assert.Equal("new", ReadMarker(World.SavePath));
        Assert.False(Directory.Exists(World.StagedSavePath));

        var aside = Array.FindAll(Directory.GetDirectories(_root), d => d.Contains(".previous-"));
        Assert.Single(aside);
        Assert.Equal("old", ReadMarker(aside[0]));
    }

    [Fact]
    public void StagedSave_WithNoSaves_IsPublished()
    {
        WriteMarker(World.StagedSavePath, "new");

        World.RecoverStagedSave();

        Assert.Equal("new", ReadMarker(World.SavePath));
        Assert.Single(Directory.GetDirectories(_root));
    }
}
