/***************************************************************************
 *                               SendQueue.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: SendQueue.cs 43 2006-01-20 05:39:57Z krrios $
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
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Server.Network
{
	public enum SendEnqueueResult
	{
		Begin,
		Delay,
		Overflow
	}

	public class SendQueue
	{
		private class Entry
		{
			public byte[] m_Buffer;
			public int m_Length;

			private Entry( byte[] buffer, int length )
			{
				m_Buffer = buffer;
				m_Length = length;
			}

			private static Stack<Entry> m_Pool = new Stack<Entry>();

			public static Entry Pool( byte[] buffer, int length )
			{
				lock ( m_Pool )
				{
					if ( m_Pool.Count == 0 )
						return new Entry( buffer, length );

					Entry e = m_Pool.Pop();

					e.m_Buffer = buffer;
					e.m_Length = length;

					return e;
				}
			}

			public static void Release( Entry e )
			{
				lock ( m_Pool )
				{
					m_Pool.Push( e );
					ReleaseBuffer( e.m_Buffer );
				}
			}
		}

		private static int m_CoalesceBufferSize = 512;
		private static BufferPool m_UnusedBuffers = new BufferPool( "Coalesced", 2048, m_CoalesceBufferSize );

		public static int CoalesceBufferSize
		{
			get{ return m_CoalesceBufferSize; }
			set
			{
				if ( m_CoalesceBufferSize == value )
					return;

				if ( m_UnusedBuffers != null )
					m_UnusedBuffers.Free();

				m_CoalesceBufferSize = value;
				m_UnusedBuffers = new BufferPool( "Coalesced", 2048, m_CoalesceBufferSize );
			}
		}

		public static byte[] GetUnusedBuffer()
		{
			return m_UnusedBuffers.AcquireBuffer();
		}

		public static void ReleaseBuffer( byte[] buffer )
		{
			if ( buffer == null )
				Console.WriteLine( "Warning: Attempting to release null packet buffer" );
			else if ( buffer.Length == m_CoalesceBufferSize )
				m_UnusedBuffers.ReleaseBuffer( buffer );
		}

		private Queue<Entry> m_Queue;

		private Entry m_Buffered;

		public bool IsFlushReady{ get{ return ( m_Queue.Count == 0 && m_Buffered != null ); } }
		public bool IsEmpty{ get{ return ( m_Queue.Count == 0 && m_Buffered == null ); } }

		public void Clear()
		{
			if ( m_Buffered != null )
			{
				Entry.Release( m_Buffered );
				m_Buffered = null;
			}

			while ( m_Queue.Count > 0 )
				Entry.Release( m_Queue.Dequeue() );
		}

		public byte[] CheckFlushReady( ref int length )
		{
			Entry buffered = m_Buffered;

			if ( m_Queue.Count == 0 && buffered != null )
			{
				m_Buffered = null;

				m_Queue.Enqueue( buffered );
				length = buffered.m_Length;
				return buffered.m_Buffer;
			}

			return null;
		}

		public SendQueue()
		{
			m_Queue = new Queue<Entry>();
		}

		public byte[] Peek( ref int length )
		{
			if ( m_Queue.Count > 0 )
			{
				Entry entry = m_Queue.Peek();

				length = entry.m_Length;
				return entry.m_Buffer;
			}

			return null;
		}

		public byte[] Dequeue( ref int length )
		{
			Entry.Release( m_Queue.Dequeue() );

			if ( m_Queue.Count > 0 )
			{
				Entry entry = m_Queue.Peek();

				length = entry.m_Length;
				return entry.m_Buffer;
			}

			return null;
		}

		private const int PendingCap = 96*1024;

		public SendEnqueueResult Enqueue( byte[] buffer, int length )
		{
			if ( buffer == null )
			{
				Console.WriteLine( "Warning: Attempting to send null packet buffer" );
				return SendEnqueueResult.Delay;
			}

			int existingBytes = ( m_Queue.Count * m_CoalesceBufferSize ) + ( m_Buffered == null ? 0 : m_Buffered.m_Length );

			if ( (existingBytes + length) > PendingCap )
				return SendEnqueueResult.Overflow;

			int offset = 0; // offset into buffer
			int remaining = length; // byte count remaining

			bool startNow = false; // should we start sending the first chunk?

			while ( remaining > 0 )
			{
				if ( m_Buffered == null ) // nothing yet buffered
					m_Buffered = Entry.Pool( GetUnusedBuffer(), 0 );

				byte[] page = m_Buffered.m_Buffer; // buffer page
				int pageSpace = page.Length - m_Buffered.m_Length; // available bytes in page
				int byteCount = ( remaining > pageSpace ? pageSpace : remaining ); // how many we can copy over

				Buffer.BlockCopy( buffer, offset, page, m_Buffered.m_Length, byteCount ); // copy the data

				// apply offsets
				m_Buffered.m_Length += byteCount;
				offset += byteCount;
				remaining -= byteCount;

				if ( m_Buffered.m_Length == page.Length ) // page full
				{
					startNow = ( startNow || m_Queue.Count == 0 );
					m_Queue.Enqueue( m_Buffered );
					m_Buffered = null;
				}
			}

			return ( startNow ? SendEnqueueResult.Begin : SendEnqueueResult.Delay );
		}
	}
}