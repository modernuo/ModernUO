/***************************************************************************
 *                           ObjectPropertyList.cs
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

using System.IO;
using System.Text;
using Server.Network;

namespace Server
{
    public interface IPropertyListObject : IEntity
    {
        ObjectPropertyList PropertyList { get; }
        OPLInfo OPLPacket { get; }

        void GetProperties(ObjectPropertyList list);
    }

    public sealed class ObjectPropertyList : Packet
    {
        private static byte[] m_Buffer = new byte[1024];
        private static readonly Encoding m_Encoding = Encoding.Unicode;

        // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
        private static readonly int[] m_StringNumbers =
        {
            1042971,
            1070722
        };

        private int m_Hash;
        private int m_Strings;

        public ObjectPropertyList(IEntity e) : base(0xD6)
        {
            EnsureCapacity(128);

            Entity = e;

            Stream.Write((short)1);
            Stream.Write(e.Serial);
            Stream.Write((byte)0);
            Stream.Write((byte)0);
            Stream.Write(e.Serial);
        }

        public IEntity Entity { get; }

        public int Hash => 0x40000000 + m_Hash;

        public int Header { get; set; }

        public string HeaderArgs { get; set; }

        public static bool Enabled { get; set; }

        public void Add(int number)
        {
            if (number == 0)
                return;

            AddHash(number);

            if (Header == 0)
            {
                Header = number;
                HeaderArgs = "";
            }

            Stream.Write(number);
            Stream.Write((short)0);
        }

        public void Terminate()
        {
            Stream.Write(0);

            Stream.Seek(11, SeekOrigin.Begin);
            Stream.Write(m_Hash);
        }

        public void AddHash(int val)
        {
            m_Hash ^= val & 0x3FFFFFF;
            m_Hash ^= (val >> 26) & 0x3F;
        }

        public void Add(int number, string arguments)
        {
            if (number == 0)
                return;

            arguments ??= "";

            if (Header == 0)
            {
                Header = number;
                HeaderArgs = arguments;
            }

            AddHash(number);
            AddHash(arguments.GetHashCode());

            Stream.Write(number);

            var byteCount = m_Encoding.GetByteCount(arguments);

            if (byteCount > m_Buffer.Length)
                m_Buffer = new byte[byteCount];

            byteCount = m_Encoding.GetBytes(arguments, 0, arguments.Length, m_Buffer, 0);

            Stream.Write((short)byteCount);
            Stream.Write(m_Buffer, 0, byteCount);
        }

        public void Add(int number, string format, object arg0)
        {
            Add(number, string.Format(format, arg0));
        }

        public void Add(int number, string format, object arg0, object arg1)
        {
            Add(number, string.Format(format, arg0, arg1));
        }

        public void Add(int number, string format, object arg0, object arg1, object arg2)
        {
            Add(number, string.Format(format, arg0, arg1, arg2));
        }

        public void Add(int number, string format, params object[] args)
        {
            Add(number, string.Format(format, args));
        }

        private int GetStringNumber() => m_StringNumbers[m_Strings++ % m_StringNumbers.Length];

        public void Add(string text)
        {
            Add(GetStringNumber(), text);
        }

        public void Add(string format, string arg0)
        {
            Add(GetStringNumber(), string.Format(format, arg0));
        }

        public void Add(string format, string arg0, string arg1)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1));
        }

        public void Add(string format, string arg0, string arg1, string arg2)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1, arg2));
        }

        public void Add(string format, params object[] args)
        {
            Add(GetStringNumber(), string.Format(format, args));
        }
    }

    public sealed class OPLInfo : Packet
    {
        /*public OPLInfo( ObjectPropertyList list ) : base( 0xBF )
        {
          EnsureCapacity( 13 );
    
          m_Stream.Write( (short) 0x10 );
          m_Stream.Write( (int) list.Entity.Serial );
          m_Stream.Write( (int) list.Hash );
        }*/

        public OPLInfo(Serial serial, int hash) : base(0xDC, 9)
        {
            Stream.Write(serial);
            Stream.Write(hash);
        }
    }
}
