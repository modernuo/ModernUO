/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BinaryFileWriter.cs                                             *
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
using System.IO;

namespace Server;

public class BinaryFileWriter : BufferWriter, IDisposable
{
    private readonly Stream _file;
    private long _position;

    public BinaryFileWriter(string filename, bool prefixStr, ConcurrentQueue<Type> types = null) :
        this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None), prefixStr, types)
    {}

    public BinaryFileWriter(Stream stream, bool prefixStr, ConcurrentQueue<Type> types = null) : base(prefixStr, types)
    {
        _file = stream;
        _position = _file.Position;
    }

    public override long Position => _position + Index;

    protected override int BufferSize => 81920;

    public override void Flush()
    {
        if (Index > 0)
        {
            _position += Index;

            _file.Write(Buffer, 0, (int)Index);
            Index = 0;
        }
    }

    public override void Close()
    {
        if (Index > 0)
        {
            Flush();
        }

        _file.Close();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Flush();

        return _position = _file.Seek(offset, origin);
    }

    public void Dispose() => Close();
}
