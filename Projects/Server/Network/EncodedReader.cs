/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EncodedReader.cs                                                *
 * Created: 2019/08/02 - Updated: 2020/08/08                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
  public ref struct EncodedReader
  {
    private BufferReader m_Reader;

    public EncodedReader(BufferReader reader) => m_Reader = reader;

    public void Trace(NetState state)
    {
      m_Reader.Trace(state);
    }

    public int ReadInt32() => m_Reader.ReadByte() != 0 ? 0 : m_Reader.ReadInt32();

    public Point3D ReadPoint3D() => m_Reader.ReadByte() != 3
      ? Point3D.Zero
      : new Point3D(m_Reader.ReadInt16(), m_Reader.ReadInt16(), m_Reader.ReadByte());

    public string ReadBigUniSafe() =>
      m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadBigUniSafe(m_Reader.ReadUInt16());

    public string ReadBigUni() =>
      m_Reader.ReadByte() != 2 ? string.Empty : m_Reader.ReadBigUni(m_Reader.ReadUInt16());
  }
}
