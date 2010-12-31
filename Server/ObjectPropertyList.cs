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

using System;
using System.Text;
using Server;
using Server.Network;

namespace Server
{
	public sealed class ObjectPropertyList : Packet
	{
		private IEntity m_Entity;
		private int m_Hash;
		private int m_Header;
		private int m_Strings;
		private string m_HeaderArgs;

		public IEntity Entity{ get{ return m_Entity; } }
		public int Hash{ get{ return 0x40000000 + m_Hash; } }

		public int Header{ get{ return m_Header; } set{ m_Header = value; } }
		public string HeaderArgs{ get{ return m_HeaderArgs; } set{ m_HeaderArgs = value; } }

		private static bool m_Enabled = false;

		public static bool Enabled{ get{ return m_Enabled; } set{ m_Enabled = value; } }

		public ObjectPropertyList( IEntity e ) : base( 0xD6 )
		{
			EnsureCapacity( 128 );

			m_Entity = e;

			m_Stream.Write( (short) 1 );
			m_Stream.Write( (int) e.Serial );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (int) e.Serial );
		}

		public void Add( int number )
		{
			if ( number == 0 )
				return;

			AddHash( number );

			if ( m_Header == 0 )
			{
				m_Header = number;
				m_HeaderArgs = "";
			}

			m_Stream.Write( number );
			m_Stream.Write( (short) 0 );
		}

		public void Terminate()
		{
			m_Stream.Write( (int) 0 );

			m_Stream.Seek( 11, System.IO.SeekOrigin.Begin );
			m_Stream.Write( (int) m_Hash );
		}

		private static byte[] m_Buffer = new byte[1024];
		private static Encoding m_Encoding = Encoding.Unicode;

		public void AddHash( int val )
		{
			m_Hash ^= (val & 0x3FFFFFF);
			m_Hash ^= (val >> 26) & 0x3F;
		}

		public void Add( int number, string arguments )
		{
			if ( number == 0 )
				return;

			if ( arguments == null )
				arguments = "";

			if ( m_Header == 0 )
			{
				m_Header = number;
				m_HeaderArgs = arguments;
			}

			AddHash( number );
			AddHash( arguments.GetHashCode() );

			m_Stream.Write( number );

			int byteCount = m_Encoding.GetByteCount( arguments );

			if ( byteCount > m_Buffer.Length )
				m_Buffer = new byte[byteCount];

			byteCount = m_Encoding.GetBytes( arguments, 0, arguments.Length, m_Buffer, 0 );

			m_Stream.Write( (short) byteCount );
			m_Stream.Write( m_Buffer, 0, byteCount );
		}

		public void Add( int number, string format, object arg0 )
		{
			Add( number, String.Format( format, arg0 ) );
		}

		public void Add( int number, string format, object arg0, object arg1 )
		{
			Add( number, String.Format( format, arg0, arg1 ) );
		}

		public void Add( int number, string format, object arg0, object arg1, object arg2 )
		{
			Add( number, String.Format( format, arg0, arg1, arg2 ) );
		}

		public void Add( int number, string format, params object[] args )
		{
			Add( number, String.Format( format, args ) );
		}

		// Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
		private static int[] m_StringNumbers = new int[]
			{
				1042971,
				1070722
			};

		private int GetStringNumber()
		{
			return m_StringNumbers[m_Strings++ % m_StringNumbers.Length];
		}

		public void Add( string text )
		{
			Add( GetStringNumber(), text );
		}

		public void Add( string format, string arg0 )
		{
			Add( GetStringNumber(), String.Format( format, arg0 ) );
		}

		public void Add( string format, string arg0, string arg1 )
		{
			Add( GetStringNumber(), String.Format( format, arg0, arg1 ) );
		}

		public void Add( string format, string arg0, string arg1, string arg2 )
		{
			Add( GetStringNumber(), String.Format( format, arg0, arg1, arg2 ) );
		}

		public void Add( string format, params object[] args )
		{
			Add( GetStringNumber(), String.Format( format, args ) );
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

		public OPLInfo( ObjectPropertyList list ) : base( 0xDC, 9 )
		{
			m_Stream.Write( (int) list.Entity.Serial );
			m_Stream.Write( (int) list.Hash );
		}
	}
}