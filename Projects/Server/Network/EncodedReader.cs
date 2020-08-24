/***************************************************************************
 *                              EncodedReader.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

namespace Server.Network
{
    public ref struct EncodedReader
    {
        private PacketReader m_Reader;

        public EncodedReader(PacketReader reader) => m_Reader = reader;

        public void Trace(NetState state)
        {
            m_Reader.Trace(state);
        }

        public int ReadInt32() => m_Reader.ReadByte() != 0 ? 0 : m_Reader.ReadInt32();

        public Point3D ReadPoint3D() => m_Reader.ReadByte() != 3
            ? Point3D.Zero
            : new Point3D(m_Reader.ReadInt16(), m_Reader.ReadInt16(), m_Reader.ReadByte());

        public string ReadUnicodeStringSafe() =>
            m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadUnicodeStringSafe(m_Reader.ReadUInt16());

        public string ReadUnicodeString() =>
            m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadUnicodeString(m_Reader.ReadUInt16());
    }
}
