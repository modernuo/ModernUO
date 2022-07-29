/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Server;

public static class AdhocPersistence
{
    /**
     * Serializes to memory synchronously. Optional buffer can be provided.
     * Note: The buffer may not be the same after returning from the function if more data is written
     * than the initial buffer can handle.
     */
    public static BufferWriter Serialize(Action<IGenericWriter> serializer)
    {
        var saveBuffer = new BufferWriter(true);
        serializer(saveBuffer);
        return saveBuffer;
    }

    /**
     * Writes a buffer to disk. This function should be called asynchronously.
     */
    public static void WriteSnapshot(string filePath, Span<byte> buffer)
    {
        var fullPath = PathUtility.GetFullPath(filePath, Core.BaseDirectory);
        var file = new FileInfo(fullPath);
        PathUtility.EnsureDirectory(file.DirectoryName);

        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        fs.Write(buffer);
    }

    public static void SerializeAndSnapshot(string filePath, Action<IGenericWriter> serializer)
    {
        var saveBuffer = Serialize(serializer);
        Task.Run(() => { WriteSnapshot(filePath, saveBuffer.Buffer.AsSpan(0, (int)saveBuffer.Position)); });
    }

    public static void Deserialize(string filePath, Action<IGenericReader> deserializer)
    {
        var fullPath = PathUtility.GetFullPath(filePath, Core.BaseDirectory);
        var file = new FileInfo(fullPath);

        if (!file.Exists)
        {
            return;
        }

        var fileLength = file.Length;
        if (fileLength == 0)
        {
            return;
        }

        string error;

        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(fullPath, FileMode.Open);
            using var stream = mmf.CreateViewStream();
            using var br = new BinaryFileReader(stream);
            deserializer(br);

            error = br.Position != fileLength
                ? $"Serialized {fileLength} bytes, but {br.Position} bytes deserialized"
                : null;
        }
        catch (Exception e)
        {
            error = e.ToString();
        }

        if (error != null)
        {
            Console.WriteLine($"***** Bad deserialize of {file.FullName} *****");
            Console.WriteLine(error);

            Console.WriteLine("Skip this file and continue? (y/n)");

            var pressedKey = Console.ReadKey(true).Key;

            if (pressedKey != ConsoleKey.Y)
            {
                throw new Exception("Deserialization failed.");
            }
        }
    }
}
