/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FileBufferWriter.cs                                             *
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
using System.Buffers;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Server;

/// <summary>
/// A <see cref="BufferWriter"/> whose staging block drains to a file when full instead of
/// growing: the full raw write path (unrolled encoded ints, in-place strings) composes into
/// memory, and the file sees large sequential positional writes. Seeks flush the staging
/// block and move the file offset, so backwards patches (e.g. the idx entity count) become
/// small positional writes. Memory-mapped writing pays soft page faults on every composed
/// page and dirty-section teardown stalls at dispose — measured ~4x slower at snapshot sizes.
/// A span larger than the staging block goes straight to the file; a string whose worst-case
/// encoding exceeds the block grows the block, since it is encoded in place.
/// </summary>
public class FileBufferWriter : BufferWriter, IDisposable
{
    private const int MinStagingSize = 256;
    private const int MaxStagingSize = 1024 * 1024; // 1MB write granularity for large files

    private readonly SafeFileHandle _handle;
    private readonly byte[] _rentedStaging;
    private long _fileOffset;    // file position where the staging block begins
    private long _fileHighWater; // logical end of file across seeks

    /// <param name="filePath">Destination file; created/truncated.</param>
    /// <param name="expectedSize">
    /// Expected total file size when known. Files at or under the staging cap never
    /// drain until close; larger files stream through a pooled block at the cap. The
    /// block comes from ArrayPool so sequential snapshot writers recycle one buffer
    /// instead of dropping a large-object allocation per file per save.
    /// </param>
    public FileBufferWriter(string filePath, long expectedSize = MaxStagingSize)
        : base(RentStaging(expectedSize), true)
    {
        _rentedStaging = Buffer;
        _handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.SequentialScan);
    }

    private static byte[] RentStaging(long expectedSize) =>
        ArrayPool<byte>.Shared.Rent((int)Math.Clamp(expectedSize, MinStagingSize, MaxStagingSize));

    public override long Position => _fileOffset + Index;

    protected override void Grow(long bytes)
    {
        if (Index > 0)
        {
            Drain();

            if (bytes <= Buffer.Length)
            {
                return;
            }
        }

        base.Grow(bytes);
    }

    public override void Write(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > Buffer.Length - Index)
        {
            if (Index > 0)
            {
                Drain();
            }

            if (bytes.Length > Buffer.Length)
            {
                WriteToFile(bytes);
                return;
            }
        }

        base.Write(bytes);
    }

    private void Drain()
    {
        WriteToFile(Buffer.AsSpan(0, (int)Index));
        Index = 0;
    }

    private void WriteToFile(ReadOnlySpan<byte> bytes)
    {
        RandomAccess.Write(_handle, bytes, _fileOffset);
        _fileOffset += bytes.Length;

        if (_fileOffset > _fileHighWater)
        {
            _fileHighWater = _fileOffset;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = Position;

        if (position > _fileHighWater)
        {
            _fileHighWater = position;
        }

        var target = origin switch
        {
            SeekOrigin.Current => position + offset,
            SeekOrigin.End     => _fileHighWater + offset,
            _                  => offset // Begin
        };

        if (target < 0)
        {
            throw new InvalidOperationException("Seek before start of file");
        }

        if (Index > 0)
        {
            Drain();
        }

        _fileOffset = target;
        return target;
    }

    public override void Close()
    {
        if (!_handle.IsClosed)
        {
            if (Index > 0)
            {
                Drain();
            }

            _handle.Dispose();

            // Safe even if an oversized item grew the staging block: growth replaced the
            // base buffer with a fresh array, so the rented one is no longer referenced.
            ArrayPool<byte>.Shared.Return(_rentedStaging);
        }
    }

    public void Dispose() => Close();
}
