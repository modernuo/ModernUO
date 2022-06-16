/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EncodedReader.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network;

public ref struct EncodedReader
{
    private CircularBufferReader _reader;

    public EncodedReader(CircularBufferReader reader) => _reader = reader;

    public void Trace(NetState state) => _reader.Trace(state);

    public int ReadInt32() => _reader.ReadByte() != 0 ? 0 : _reader.ReadInt32();

    public Point3D ReadPoint3D() => _reader.ReadByte() != 3
        ? Point3D.Zero
        : new Point3D(_reader.ReadInt16(), _reader.ReadInt16(), _reader.ReadByte());

    public string ReadUnicodeStringSafe() =>
        _reader.ReadByte() != 2 ? string.Empty : _reader.ReadBigUniSafe(_reader.ReadUInt16());

    public string ReadUnicodeString() =>
        _reader.ReadByte() != 2 ? string.Empty : _reader.ReadBigUni(_reader.ReadUInt16());
}
