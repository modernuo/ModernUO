/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CopySource.cs                                                   *
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

namespace Server;

/// <summary>
/// Read-only memory map of the previous committed save file, from which the snapshot writer
/// copies the records of entities that were not serialized during the freeze and reads the
/// previous bytes of entities serialized for verification. Lives only for one
/// <c>WriteSnapshot</c> call, on the writer thread, and is closed before the previous save is
/// archived.
/// </summary>
internal sealed unsafe class CopySource : IDisposable
{
    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _accessor;
    private byte* _pointer;

    public long Length { get; }

    private CopySource(string path, long length)
    {
        Length = length;
        _file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
    }

    /// <summary>
    /// Opens <paramref name="path" /> when it still is the file the placements refer to: same
    /// length and last write time as recorded at commit. Returns null otherwise; the caller
    /// decides whether that is fatal (a save that must copy) or benign (verification only).
    /// </summary>
    public static CopySource TryOpen(string path, long expectedLength, DateTime expectedWriteTimeUtc)
    {
        if (expectedLength <= 0)
        {
            return null;
        }

        var file = new FileInfo(path);
        if (!file.Exists || file.Length != expectedLength || file.LastWriteTimeUtc != expectedWriteTimeUtc)
        {
            return null;
        }

        return new CopySource(path, expectedLength);
    }

    public ReadOnlySpan<byte> Slice(long position, int length)
    {
        if (position < 0 || length < 0 || position + length > Length)
        {
            throw new InvalidOperationException(
                $"Placement [{position}, {position + length}) is outside the {Length}-byte copy source."
            );
        }

        return new ReadOnlySpan<byte>(_pointer + position, length);
    }

    /// <summary>Streams [position, position + length) into the stream in bounded pieces.</summary>
    public void CopyTo(Stream destination, long position, long length)
    {
        const int piece = 256 * 1024 * 1024;

        while (length > 0)
        {
            var count = (int)Math.Min(piece, length);
            destination.Write(Slice(position, count));
            position += count;
            length -= count;
        }
    }

    public void Dispose()
    {
        if (_pointer != null)
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _pointer = null;
        }

        _accessor.Dispose();
        _file.Dispose();
    }
}
