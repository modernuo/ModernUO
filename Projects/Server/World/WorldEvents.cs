using System;
using System.Runtime.CompilerServices;

namespace Server;

public class WorldSavePostSnapshotEventArgs
{
    public string OldSavePath { get; }
    public string NewSavePath { get; }

    public WorldSavePostSnapshotEventArgs(string oldSavePath, string newSavePath)
    {
        OldSavePath = oldSavePath;
        NewSavePath = newSavePath;
    }
}

public static partial class EventSink
{
    public static event Action WorldLoad;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeWorldLoad() => WorldLoad?.Invoke();

    public static event Action WorldSave;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeWorldSave() => WorldSave?.Invoke();

    public static event Action<WorldSavePostSnapshotEventArgs> WorldSavePostSnapshot;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeWorldSavePostSnapshot(string oldSavePath, string newSavePath) =>
        WorldSavePostSnapshot?.Invoke(new WorldSavePostSnapshotEventArgs(oldSavePath, newSavePath));
}
