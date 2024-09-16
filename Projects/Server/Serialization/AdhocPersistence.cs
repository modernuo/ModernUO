/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AdhocPersistence.cs                                             *
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;

namespace Server;

public static class AdhocPersistence
{
    /**
     * Serializes to memory.
     */
    public static IGenericWriter SerializeToBuffer(Action<IGenericWriter> serializer, ConcurrentQueue<Type> types = null)
    {
        var saveBuffer = new BufferWriter(true, types);
        serializer(saveBuffer);
        return saveBuffer;
    }

    /**
     * Deserializes from a buffer.
     */
    public static IGenericReader DeserializeFromBuffer(
        byte[] buffer, Action<IGenericReader> deserializer, Dictionary<ulong, string> typesDb = null
    )
    {
        var reader = new BufferReader(buffer, typesDb);
        deserializer(reader);
        return reader;
    }

    /**
     * Serializes to a Memory Mapped file synchronously, then flushes to the file asynchronously.
     */
    public static void SerializeAndSnapshot(
        string filePath, Action<IGenericWriter> serializer, long sizeHint = 1024 * 1024 * 32
    )
    {
        var fullPath = PathUtility.GetFullPath(filePath, Core.BaseDirectory);
        PathUtility.EnsureDirectory(Path.GetDirectoryName(fullPath));
        HashSet<Type> typesSet = [];
        var writer = new MemoryMapFileWriter(new FileStream(filePath, FileMode.Create), sizeHint, typesSet);
        serializer(writer);

        Task.Run(
            () =>
            {
                var fs = writer.FileStream;

                writer.Dispose();
                fs.Dispose();

                Persistence.WriteSerializedTypesSnapshot(Path.GetDirectoryName(fullPath), typesSet);
            },
            Core.ClosingTokenSource.Token
        );
    }

    public static unsafe void Deserialize(string filePath, Action<IGenericReader> deserializer)
    {
        var fullPath = PathUtility.GetFullPath(filePath, Core.BaseDirectory);
        var file = new FileInfo(fullPath);

        if (!file.Exists || file.Length == 0)
        {
            return;
        }

        var fileLength = file.Length;

        string error;

        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(fullPath, FileMode.Open);
            using var accessor = mmf.CreateViewStream();

            byte* ptr = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            UnmanagedDataReader dataReader = new UnmanagedDataReader(ptr, accessor.Length);
            deserializer(dataReader);

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

            if (!ConsoleInputHandler.ReadLine().InsensitiveEquals("y"))
            {
                throw new Exception("Deserialization failed.");
            }
        }
    }
}
