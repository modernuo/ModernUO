/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ReadOnlyFileData.cs                                             *
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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Server;

/// <summary>
/// A whole file's bytes, read-only, at a stable pointer until disposed. Linux and macOS map the
/// file; Windows reads it into native memory, which measured faster there than mapping, while
/// mapping is faster on Linux with the page cache warm or cold.
/// </summary>
/// <remarks>
/// The pointer is invalid after <see cref="Dispose" />. Code that hands it to another thread
/// must keep this instance undisposed until that thread is done with it.
/// </remarks>
public sealed unsafe class ReadOnlyFileData : IDisposable
{
    private const int MaxReadChunk = 1024 * 1024 * 1024;

    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _view;
    private byte* _native;
    private bool _pointerAcquired;

    public byte* Pointer { get; private set; }

    public long Length { get; }

    public ReadOnlyFileData(string path)
    {
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        Length = RandomAccess.GetLength(handle);

        if (Length == 0)
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                ReadIntoNativeMemory(handle);
            }
            else
            {
                Map(path);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void ReadIntoNativeMemory(SafeFileHandle handle)
    {
        _native = (byte*)NativeMemory.Alloc((nuint)Length);

        for (long offset = 0; offset < Length;)
        {
            var chunk = (int)Math.Min(MaxReadChunk, Length - offset);
            var read = RandomAccess.Read(handle, new Span<byte>(_native + offset, chunk), offset);

            if (read <= 0)
            {
                throw new EndOfStreamException($"File ended after {offset} of {Length} bytes.");
            }

            offset += read;
        }

        Pointer = _native;
    }

    private void Map(string path)
    {
        _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _view = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);

        byte* ptr = null;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        _pointerAcquired = true;

        Pointer = ptr + _view.PointerOffset;
    }

    /// <summary>
    /// A reader over the whole file, bounded by its length.
    /// </summary>
    public UnmanagedDataReader CreateReader(Dictionary<ulong, string> typesDb = null, Encoding encoding = null) =>
        new(Pointer, Length, typesDb, encoding);

    public void Dispose()
    {
        Pointer = null;

        if (_native != null)
        {
            NativeMemory.Free(_native);
            _native = null;
        }

        if (_pointerAcquired)
        {
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
            _pointerAcquired = false;
        }

        _view?.Dispose();
        _view = null;
        _mmf?.Dispose();
        _mmf = null;
    }
}
