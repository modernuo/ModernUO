using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Server.Tests;

/// <summary>
/// A private save pipeline: a chunk source with its own serialization workers, optionally installed as
/// World's workers, optionally a scratch directory and an extra assembly visible to the type loader.
/// Dispose unregisters every tracked persistence, exits the workers and restores every static it swapped.
/// </summary>
internal sealed class PersistencePipelineScope : IDisposable
{
    private readonly List<Persistence> _persistences = [];
    private readonly SerializationThreadWorker[] _previousWorkers;
    private readonly Assembly[] _previousAssemblies;
    private readonly DateTime _previousSaveStart;
    private readonly bool _installedWorkers;
    private readonly bool _swappedAssemblies;

    /// <param name="workerCount">Workers to create, each with an allocated heap.</param>
    /// <param name="installWorkers">Install the workers as World's, which WriteSnapshot reads segments from.</param>
    /// <param name="visibleAssembly">An assembly the loader must resolve types from by hash.</param>
    /// <param name="directoryPrefix">Creates a unique scratch directory under the temp path when set.</param>
    public PersistencePipelineScope(
        int workerCount,
        bool installWorkers = false,
        Assembly visibleAssembly = null,
        string directoryPrefix = null
    )
    {
        _previousSaveStart = World.SaveStartTime;

        Source = new SerializationChunkSource();
        Workers = new SerializationThreadWorker[workerCount];
        for (var i = 0; i < Workers.Length; i++)
        {
            Workers[i] = new SerializationThreadWorker(i, Source);
            Workers[i].AllocateHeap();
        }

        if (installWorkers)
        {
            _installedWorkers = true;
            _previousWorkers = World._threadWorkers;
            World._threadWorkers = Workers;
        }

        if (visibleAssembly != null)
        {
            _swappedAssemblies = true;
            _previousAssemblies = AssemblyHandler.Assemblies;
            AssemblyHandler.Assemblies = [.. _previousAssemblies ?? [], visibleAssembly];
        }

        if (directoryPrefix != null)
        {
            Directory = Path.Combine(Path.GetTempPath(), $"{directoryPrefix}-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(Directory);
        }
    }

    public SerializationChunkSource Source { get; }

    public SerializationThreadWorker[] Workers { get; }

    /// <summary>The scratch directory, or null when none was requested.</summary>
    public string Directory { get; }

    /// <summary>Tracks a persistence so it leaves the static registry on dispose.</summary>
    public T Track<T>(T persistence) where T : Persistence
    {
        _persistences.Add(persistence);
        return persistence;
    }

    public void WakeWorkers()
    {
        for (var i = 0; i < Workers.Length; i++)
        {
            Workers[i].Wake();
        }
    }

    public void SleepWorkers()
    {
        for (var i = 0; i < Workers.Length; i++)
        {
            Workers[i].Sleep();
        }
    }

    /// <summary>Mirrors World.PauseSerializationThreads: publish the partial chunk, then wait for the drain.</summary>
    public void FlushAndSleepWorkers()
    {
        Source.Flush();
        SleepWorkers();
    }

    public void Dispose()
    {
        World.SaveStartTime = _previousSaveStart;

        for (var i = 0; i < _persistences.Count; i++)
        {
            _persistences[i]?.Unregister();
        }

        for (var i = 0; i < Workers.Length; i++)
        {
            Workers[i].Exit();
        }

        if (_installedWorkers)
        {
            World._threadWorkers = _previousWorkers;
        }

        if (_swappedAssemblies)
        {
            AssemblyHandler.Assemblies = _previousAssemblies;
        }

        if (Directory != null)
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }
}
