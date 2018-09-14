/***************************************************************************
 *                               ByteQueue.cs
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

namespace Server.Network
{
	public class ByteQueue
	{
		private int m_Head;
		private int m_Tail;

		private byte[] m_Buffer;

		public int Length { get; private set; }

		public ByteQueue()
		{
			m_Buffer = new byte[2048];
		}

		public void Clear()
		{
			m_Head = 0;
			m_Tail = 0;
			Length = 0;
		}

		private void SetCapacity( int capacity ) 
		{
			byte[] newBuffer = new byte[capacity];

			if ( Length > 0 )
			{
				if ( m_Head < m_Tail )
				{
					Buffer.BlockCopy( m_Buffer, m_Head, newBuffer, 0, Length );
				}
				else
				{
					Buffer.BlockCopy( m_Buffer, m_Head, newBuffer, 0, m_Buffer.Length - m_Head );
					Buffer.BlockCopy( m_Buffer, 0, newBuffer, m_Buffer.Length - m_Head, m_Tail );
				}
			}

			m_Head = 0;
			m_Tail = Length;
			m_Buffer = newBuffer;
		}

		public byte GetPacketID()
		{
			if ( Length >= 1 )
				return m_Buffer[m_Head];

			return 0xFF;
		}

		public int GetPacketLength()
		{
			if ( Length >= 3 )
				return (m_Buffer[(m_Head + 1) % m_Buffer.Length] << 8) | m_Buffer[(m_Head + 2) % m_Buffer.Length];

			return 0;
		}

		public int Dequeue( byte[] buffer, int offset, int size )
		{
			if ( size > Length )
				size = Length;

			if ( size == 0 )
				return 0;

			if ( m_Head < m_Tail )
			{
				Buffer.BlockCopy( m_Buffer, m_Head, buffer, offset, size );
			}
			else
			{
				int rightLength = ( m_Buffer.Length - m_Head );

				if ( rightLength >= size )
				{
					Buffer.BlockCopy( m_Buffer, m_Head, buffer, offset, size );
				}
				else
				{
					Buffer.BlockCopy( m_Buffer, m_Head, buffer, offset, rightLength );
					Buffer.BlockCopy( m_Buffer, 0, buffer, offset + rightLength, size - rightLength );
				}
			}

			m_Head = ( m_Head + size ) % m_Buffer.Length;
			Length -= size;

			if ( Length == 0 )
			{
				m_Head = 0;
				m_Tail = 0;
			}

			return size;
		}

		public void Enqueue( byte[] buffer, int offset, int size )
		{
			if ( (Length + size) > m_Buffer.Length )
				SetCapacity( (Length + size + 2047) & ~2047 );

			if ( m_Head < m_Tail )
			{
				int rightLength = ( m_Buffer.Length - m_Tail );

				if ( rightLength >= size )
				{
					Buffer.BlockCopy( buffer, offset, m_Buffer, m_Tail, size );
				}
				else
				{
					Buffer.BlockCopy( buffer, offset, m_Buffer, m_Tail, rightLength );
					Buffer.BlockCopy( buffer, offset + rightLength, m_Buffer, 0, size - rightLength );
				}
			}
			else
			{
				Buffer.BlockCopy( buffer, offset, m_Buffer, m_Tail, size );
			}

			m_Tail = ( m_Tail + size ) % m_Buffer.Length;
			Length += size;
		}
	}
}