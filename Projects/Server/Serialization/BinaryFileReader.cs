/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BinaryFileReader.cs                                             *
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
using System.Runtime.CompilerServices;

namespace Server
{
    public class BinaryFileReader : IGenericReader, IDisposable
    {
        private readonly BinaryReader _reader;

        public BinaryFileReader(BinaryReader br) => _reader = br;

        public BinaryFileReader(Stream stream) => _reader = new BinaryReader(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Close() => _reader.Close();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(bool intern = false)
        {
            var str = _reader.ReadString();
            return intern ? Utility.Intern(str) : str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong() => _reader.ReadInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadULong() => _reader.ReadUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt() => _reader.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt() => _reader.ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadShort() => _reader.ReadInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort() => _reader.ReadUInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble() => _reader.ReadDouble();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat() => _reader.ReadSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => _reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => _reader.ReadSByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => _reader.ReadBoolean();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Serial ReadSerial() => (Serial)_reader.ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(Span<byte> buffer) => _reader.Read(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Seek(long offset, SeekOrigin origin) => _reader.BaseStream.Seek(offset, origin);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => Close();
    }
}
