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

using System;
using System.Text;
using System.IO;

namespace Server.Network
{
	public class EncodedReader
	{
		private PacketReader m_Reader;

		public EncodedReader( PacketReader reader )
		{
			m_Reader = reader;
		}

		public byte[] Buffer
		{
			get
			{
				return m_Reader.Buffer;
			}
		}

		public void Trace( NetState state )
		{
			m_Reader.Trace( state );
		}

		public int ReadInt32()
		{
			if ( m_Reader.ReadByte() != 0 )
				return 0;

			return m_Reader.ReadInt32();
		}

		public Point3D ReadPoint3D()
		{
			if ( m_Reader.ReadByte() != 3 )
				return Point3D.Zero;

			return new Point3D( m_Reader.ReadInt16(), m_Reader.ReadInt16(), m_Reader.ReadByte() );
		}

		public string ReadUnicodeStringSafe()
		{
			if ( m_Reader.ReadByte() != 2 )
				return "";

			int length = m_Reader.ReadUInt16();

			return m_Reader.ReadUnicodeStringSafe( length );
		}

		public string ReadUnicodeString()
		{
			if ( m_Reader.ReadByte() != 2 )
				return "";

			int length = m_Reader.ReadUInt16();

			return m_Reader.ReadUnicodeString( length );
		}
	}
}