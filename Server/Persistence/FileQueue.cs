/***************************************************************************
 *                               FileQueue.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using Server;
using Server.Network;

namespace Server {
	public delegate void FileCommitCallback( FileQueue.Chunk chunk );

	public sealed class FileQueue : IDisposable {
		public sealed class Chunk {
			private FileQueue owner;
			private int slot;

			private byte[] buffer;
			private int offset;
			private int size;

			public byte[] Buffer {
				get {
					return buffer;
				}
			}

			public int Offset {
				get {
					return 0;
				}
			}

			public int Size {
				get {
					return size;
				}
			}

			public Chunk( FileQueue owner, int slot, byte[] buffer, int offset, int size ) {
				this.owner = owner;
				this.slot = slot;

				this.buffer = buffer;
				this.offset = offset;
				this.size = size;
			}

			public void Commit() {
				owner.Commit( this, this.slot );
			}
		}

		private struct Page {
			public byte[] buffer;
			public int length;
		}

		private static int bufferSize;
		private static BufferPool bufferPool;

		static FileQueue() {
			bufferSize = FileOperations.BufferSize;
			bufferPool = new BufferPool( "File Buffers", 64, bufferSize );
		}

		private object syncRoot;

		private Chunk[] active;
		private int activeCount;

		private Queue<Page> pending;
		private Page buffered;

		private FileCommitCallback callback;

		private ManualResetEvent idle;

		private long position;

		public long Position {
			get {
				return position;
			}
		}

		public FileQueue( int concurrentWrites, FileCommitCallback callback ) {
			if ( concurrentWrites < 1 ) {
				throw new ArgumentOutOfRangeException( "concurrentWrites" );
			} else if ( bufferSize < 1 ) {
				throw new ArgumentOutOfRangeException( "bufferSize" );
			} else if ( callback == null ) {
				throw new ArgumentNullException( "callback" );
			}

			this.syncRoot = new object();

			this.active = new Chunk[concurrentWrites];
			this.pending = new Queue<Page>();

			this.callback = callback;

			this.idle = new ManualResetEvent( true );
		}

		private void Append( Page page ) {
			lock ( syncRoot ) {
				if ( activeCount == 0 ) {
					idle.Reset();
				}

				++activeCount;

				for ( int slot = 0; slot < active.Length; ++slot ) {
					if ( active[slot] == null ) {
						active[slot] = new Chunk( this, slot, page.buffer, 0, page.length );

						callback( active[slot] );

						return;
					}
				}

				pending.Enqueue( page );
			}
		}

		public void Dispose() {
			if ( idle != null ) {
				idle.Close();
				idle = null;
			}
		}

		public void Flush() {
			if ( buffered.buffer != null ) {
				Append( buffered );

				buffered.buffer = null;
				buffered.length = 0;
			}

			/*lock ( syncRoot ) {
				if ( pending.Count > 0 ) {
					idle.Reset();
				}

				for ( int slot = 0; slot < active.Length && pending.Count > 0; ++slot ) {
					if ( active[slot] == null ) {
						Page page = pending.Dequeue();

						active[slot] = new Chunk( this, slot, page.buffer, 0, page.length );

						++activeCount;

						callback( active[slot] );
					}
				}
			}*/

			idle.WaitOne();
		}

		private void Commit( Chunk chunk, int slot ) {
			if ( slot < 0 || slot >= active.Length ) {
				throw new ArgumentOutOfRangeException( "slot" );
			}

			lock ( syncRoot ) {
				if ( active[slot] != chunk ) {
					throw new ArgumentException();
				}

				bufferPool.ReleaseBuffer( chunk.Buffer );

				if ( pending.Count > 0 ) {
					Page page = pending.Dequeue();

					active[slot] = new Chunk( this, slot, page.buffer, 0, page.length );

					callback( active[slot] );
				} else {
					active[slot] = null;
				}

				--activeCount;

				if ( activeCount == 0 ) {
					idle.Set();
				}
			}
		}

		public void Enqueue( byte[] buffer, int offset, int size ) {
			if ( buffer == null ) {
				throw new ArgumentNullException( "buffer" );
			} else if ( offset < 0 ) {
				throw new ArgumentOutOfRangeException( "offset" );
			} else if ( size < 0 ) {
				throw new ArgumentOutOfRangeException( "size" );
			} else if ( ( buffer.Length - offset ) < size ) {
				throw new ArgumentException();
			}

			position += size;

			while ( size > 0 ) {
				if ( buffered.buffer == null ) { // nothing yet buffered
					buffered.buffer = bufferPool.AcquireBuffer();
				}

				byte[] page = buffered.buffer; // buffer page
				int pageSpace = page.Length - buffered.length; // available bytes in page
				int byteCount = ( size > pageSpace ? pageSpace : size ); // how many bytes we can copy over

				Buffer.BlockCopy( buffer, offset, page, buffered.length, byteCount );

				buffered.length += byteCount;
				offset += byteCount;
				size -= byteCount;

				if ( buffered.length == page.Length ) { // page full
					Append( buffered );

					buffered.buffer = null;
					buffered.length = 0;
				}
			}
		}
	}
}