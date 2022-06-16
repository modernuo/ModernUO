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
    private CircularBufferReader m_Reader;

    public EncodedReader(CircularBufferReader reader) => m_Reader = reader;

    public void Trace(NetState state)
    {
        m_Reader.Trace(state);
    }

    public int ReadInt32() => m_Reader.ReadByte() != 0 ? 0 : m_Reader.ReadInt32();

    public Point3D ReadPoint3D() => m_Reader.ReadByte() != 3
        ? Point3D.Zero
        : new Point3D(m_Reader.ReadInt16(), m_Reader.ReadInt16(), m_Reader.ReadByte());

    public string ReadUnicodeStringSafe() =>
        m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadBigUniSafe(m_Reader.ReadUInt16());

    public string ReadUnicodeString() =>
        m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadBigUni(m_Reader.ReadUInt16());
}
