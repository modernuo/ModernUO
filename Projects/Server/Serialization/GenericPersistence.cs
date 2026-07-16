/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenericPersistence.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Server;

public abstract class GenericPersistence : Persistence, IGenericSerializable
{
    public string Name { get; }
    public string SaveFilePath { get; protected set; } // "<Folder>/<System>.bin"

    // Placement of the self-payload in the worker heaps for the most recent save. Only
    // persistences carry placement state — entities are located through the per-worker
    // segment logs instead.
    private protected byte _selfThread;
    private protected int _selfPosition;
    private protected int _selfLength;

    internal void SetSelfPlacement(byte thread, int position, int length)
    {
        _selfThread = thread;
        _selfPosition = position;
        _selfLength = length;
    }

    private long _loadedFileLength;

    // Scheduling estimate only: previous save's payload size, or the loaded file size before the first save.
    internal long EstimatedSize => _selfLength > 0 ? _selfLength : _loadedFileLength;

    public GenericPersistence(string name, int priority) : base(priority)
    {
        Name = name;
        SaveFilePath = Path.Combine(Name, $"{Name}.bin");
    }

    public override void Serialize()
    {
        // Always a dedicated chunk: self-payloads can be arbitrarily large and must not
        // ride inside a shared chunk where one worker would serialize them plus the chunk.
        World.PushSingleToCache(this);
    }

    public override void WriteSnapshot(string savePath)
    {
        if (_selfLength == 0)
        {
            return;
        }

        var file = Path.Combine(savePath, SaveFilePath);
        var dir = Path.GetDirectoryName(file);
        PathUtility.EnsureDirectory(dir);

        var threads = World._threadWorkers;

        using var binFs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);

        binFs.Write(threads[_selfThread].GetHeap(_selfPosition, _selfLength));
    }

    public override unsafe void Deserialize(string savePath, Dictionary<ulong, string> typesDb)
    {
        // Assume savePath has the Core.BaseDirectory already prepended
        var dataPath = Path.GetFullPath(SaveFilePath, savePath);
        var file = new FileInfo(dataPath);

        if (!file.Exists || file.Length <= 0)
        {
            return;
        }

        var fileLength = file.Length;
        _loadedFileLength = fileLength;

        string error;

        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(dataPath, FileMode.Open);
            using var accessor = mmf.CreateViewStream();

            byte* ptr = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            var dataReader = new UnmanagedDataReader(ptr, accessor.Length, typesDb);
            Deserialize(dataReader);

            error = dataReader.Position != fileLength
                ? $"Serialized {fileLength} bytes, but {dataReader.Position} bytes deserialized"
                : null;

            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
        catch (Exception e)
        {
            error = e.ToString();
        }

        if (error != null)
        {
            Console.WriteLine($"***** Bad deserialize of {file.FullName} *****");
            Console.WriteLine(error);

            Console.Write("Skip this file and continue? (y/n): ");
            var y = ConsoleInputHandler.ReadLine();

            if (!y.InsensitiveEquals("y"))
            {
                throw new Exception("Deserialization failed.");
            }
        }
    }

    public abstract void Serialize(IGenericWriter writer);
    public abstract void Deserialize(IGenericReader reader);
}
