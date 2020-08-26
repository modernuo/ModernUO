/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: BinaryFileReader.cs                                             *
 * Created: 2019/12/30 - Updated: 2020/01/18                             *
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Guilds;

namespace Server
{
    public sealed class BinaryFileReader : IGenericReader
    {
        private readonly BinaryReader m_File;

        public BinaryFileReader(BinaryReader br) => m_File = br;

        public long Position => m_File.BaseStream.Position;

        public string ReadString() => ReadByte() != 0 ? m_File.ReadString() : null;

        public DateTime ReadDeltaTime()
        {
            var ticks = m_File.ReadInt64();
            var now = DateTime.UtcNow.Ticks;

            if (ticks > 0 && ticks + now < 0)
                return DateTime.MaxValue;
            if (ticks < 0 && ticks + now < 0)
                return DateTime.MinValue;

            try
            {
                return new DateTime(now + ticks);
            }
            catch
            {
                return ticks > 0 ? DateTime.MaxValue : DateTime.MinValue;
            }
        }

        public IPAddress ReadIPAddress() => new IPAddress(m_File.ReadInt64());

        public int ReadEncodedInt()
        {
            int v = 0, shift = 0;
            byte b;

            do
            {
                b = m_File.ReadByte();
                v |= (b & 0x7F) << shift;
                shift += 7;
            } while (b >= 0x80);

            return v;
        }

        public DateTime ReadDateTime() => new DateTime(m_File.ReadInt64());

        public DateTimeOffset ReadDateTimeOffset()
        {
            var ticks = m_File.ReadInt64();
            var offset = new TimeSpan(m_File.ReadInt64());

            return new DateTimeOffset(ticks, offset);
        }

        public TimeSpan ReadTimeSpan() => new TimeSpan(m_File.ReadInt64());

        public decimal ReadDecimal() => m_File.ReadDecimal();

        public long ReadLong() => m_File.ReadInt64();

        public ulong ReadULong() => m_File.ReadUInt64();

        public int ReadInt() => m_File.ReadInt32();

        public uint ReadUInt() => m_File.ReadUInt32();

        public short ReadShort() => m_File.ReadInt16();

        public ushort ReadUShort() => m_File.ReadUInt16();

        public double ReadDouble() => m_File.ReadDouble();

        public float ReadFloat() => m_File.ReadSingle();

        public char ReadChar() => m_File.ReadChar();

        public byte ReadByte() => m_File.ReadByte();

        public sbyte ReadSByte() => m_File.ReadSByte();

        public bool ReadBool() => m_File.ReadBoolean();

        public Point3D ReadPoint3D() => new Point3D(ReadInt(), ReadInt(), ReadInt());

        public Point2D ReadPoint2D() => new Point2D(ReadInt(), ReadInt());

        public Rectangle2D ReadRect2D() => new Rectangle2D(ReadPoint2D(), ReadPoint2D());

        public Rectangle3D ReadRect3D() => new Rectangle3D(ReadPoint3D(), ReadPoint3D());

        public Map ReadMap() => Map.Maps[ReadByte()];

        public IEntity ReadEntity()
        {
            Serial serial = ReadUInt();
            return World.FindEntity(serial) ?? new Entity(serial, new Point3D(0, 0, 0), Map.Internal);
        }

        public Item ReadItem() => World.FindItem(ReadUInt());

        public Mobile ReadMobile() => World.FindMobile(ReadUInt());

        public BaseGuild ReadGuild() => BaseGuild.Find(ReadUInt());

        public T ReadItem<T>() where T : Item => ReadItem() as T;

        public T ReadMobile<T>() where T : Mobile => ReadMobile() as T;

        public T ReadGuild<T>() where T : BaseGuild => ReadGuild() as T;

        public List<Item> ReadStrongItemList() => ReadStrongItemList<Item>();

        public List<T> ReadStrongItemList<T>() where T : Item
        {
            var count = ReadInt();

            if (count > 0)
            {
                var list = new List<T>(count);

                for (var i = 0; i < count; ++i)
                    if (ReadItem() is T item)
                        list.Add(item);

                return list;
            }

            return new List<T>();
        }

        public HashSet<Item> ReadItemSet() => ReadItemSet<Item>();

        public HashSet<T> ReadItemSet<T>() where T : Item
        {
            var count = ReadInt();

            if (count > 0)
            {
                var set = new HashSet<T>();

                for (var i = 0; i < count; ++i)
                    if (ReadItem() is T item)
                        set.Add(item);

                return set;
            }

            return new HashSet<T>();
        }

        public List<Mobile> ReadStrongMobileList() => ReadStrongMobileList<Mobile>();

        public List<T> ReadStrongMobileList<T>() where T : Mobile
        {
            var count = ReadInt();

            if (count > 0)
            {
                var list = new List<T>(count);

                for (var i = 0; i < count; ++i)
                    if (ReadMobile() is T m)
                        list.Add(m);

                return list;
            }

            return new List<T>();
        }

        public HashSet<Mobile> ReadMobileSet() => ReadMobileSet<Mobile>();

        public HashSet<T> ReadMobileSet<T>() where T : Mobile
        {
            var count = ReadInt();

            if (count > 0)
            {
                var set = new HashSet<T>();

                for (var i = 0; i < count; ++i)
                    if (ReadMobile() is T item)
                        set.Add(item);

                return set;
            }

            return new HashSet<T>();
        }

        public List<BaseGuild> ReadStrongGuildList() => ReadStrongGuildList<BaseGuild>();

        public List<T> ReadStrongGuildList<T>() where T : BaseGuild
        {
            var count = ReadInt();

            if (count > 0)
            {
                var list = new List<T>(count);

                for (var i = 0; i < count; ++i)
                    if (ReadGuild() is T g)
                        list.Add(g);

                return list;
            }

            return new List<T>();
        }

        public HashSet<BaseGuild> ReadGuildSet() => ReadGuildSet<BaseGuild>();

        public HashSet<T> ReadGuildSet<T>() where T : BaseGuild
        {
            var count = ReadInt();

            if (count > 0)
            {
                var set = new HashSet<T>();

                for (var i = 0; i < count; ++i)
                    if (ReadGuild() is T item)
                        set.Add(item);

                return set;
            }

            return new HashSet<T>();
        }

        public Race ReadRace() => Race.Races[ReadByte()];

        public bool End() => m_File.PeekChar() == -1;

        public void Close()
        {
            m_File.Close();
        }

        public long Seek(long offset, SeekOrigin origin) => m_File.BaseStream.Seek(offset, origin);
    }
}
