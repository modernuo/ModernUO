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
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace Server
{
    public delegate void FileCommitCallback(FileQueue.Chunk chunk);

    public sealed class FileQueue : IDisposable
    {
        private static readonly int bufferSize;

        private readonly Chunk[] active;

        private readonly FileCommitCallback callback;

        private readonly Queue<Page> pending;

        private readonly object syncRoot;
        private int activeCount;
        private Page buffered;

        private ManualResetEvent idle;

        static FileQueue() => bufferSize = FileOperations.BufferSize;

        public FileQueue(int concurrentWrites, FileCommitCallback callback)
        {
            if (concurrentWrites < 1) throw new ArgumentOutOfRangeException(nameof(concurrentWrites));

            if (bufferSize < 1)
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException(nameof(FileOperations.BufferSize));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

            syncRoot = new object();

            active = new Chunk[concurrentWrites];
            pending = new Queue<Page>();

            this.callback = callback;

            idle = new ManualResetEvent(true);
        }

        public long Position { get; private set; }

        public void Dispose()
        {
            if (idle != null)
            {
                idle.Close();
                idle = null;
            }
        }

        private void Append(Page page)
        {
            lock (syncRoot)
            {
                if (activeCount == 0) idle.Reset();

                ++activeCount;

                for (var slot = 0; slot < active.Length; ++slot)
                    if (active[slot] == null)
                    {
                        active[slot] = new Chunk(this, slot, page.buffer, 0, page.length);

                        callback(active[slot]);

                        return;
                    }

                pending.Enqueue(page);
            }
        }

        public void Flush()
        {
            if (buffered.buffer != null)
            {
                Append(buffered);

                buffered.buffer = null;
                buffered.length = 0;
            }

            /*lock ( syncRoot ) {
              if (pending.Count > 0 ) {
                idle.Reset();
              }
      
              for ( int slot = 0; slot < active.Length && pending.Count > 0; ++slot ) {
                if (active[slot] == null ) {
                  Page page = pending.Dequeue();
      
                  active[slot] = new Chunk( this, slot, page.buffer, 0, page.length );
      
                  ++activeCount;
      
                  callback( active[slot] );
                }
              }
            }*/

            idle.WaitOne();
        }

        private void Commit(Chunk chunk, int slot)
        {
            if (slot < 0 || slot >= active.Length) throw new ArgumentOutOfRangeException(nameof(slot));

            lock (syncRoot)
            {
                if (active[slot] != chunk) throw new ArgumentException("active slot is not the current chunk");

                ArrayPool<byte>.Shared.Return(chunk.Buffer);

                if (pending.Count > 0)
                {
                    var page = pending.Dequeue();

                    active[slot] = new Chunk(this, slot, page.buffer, 0, page.length);

                    callback(active[slot]);
                }
                else
                {
                    active[slot] = null;
                }

                --activeCount;

                if (activeCount == 0) idle.Set();
            }
        }

        public void Enqueue(byte[] buffer, int offset, int size)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (buffer.Length - offset < size) throw new ArgumentOutOfRangeException(nameof(offset));

            Position += size;

            while (size > 0)
            {
                buffered.buffer ??= ArrayPool<byte>.Shared.Rent(bufferSize);

                var page = buffered.buffer;                          // buffer page
                var pageSpace = page.Length - buffered.length;       // available bytes in page
                var byteCount = size > pageSpace ? pageSpace : size; // how many bytes we can copy over

                Buffer.BlockCopy(buffer, offset, page, buffered.length, byteCount);

                buffered.length += byteCount;
                offset += byteCount;
                size -= byteCount;

                if (buffered.length == page.Length)
                {
                    // page full
                    Append(buffered);

                    buffered.buffer = null;
                    buffered.length = 0;
                }
            }
        }

        public sealed class Chunk
        {
            private readonly FileQueue m_Owner;
            private readonly int m_Slot;

            public Chunk(FileQueue owner, int slot, byte[] buffer, int offset, int size)
            {
                m_Owner = owner;
                m_Slot = slot;

                Buffer = buffer;
                Offset = offset;
                Size = size;
            }

            public byte[] Buffer { get; }

            public int Offset { get; }

            public int Size { get; }

            public void Commit()
            {
                m_Owner.Commit(this, m_Slot);
            }
        }

        private struct Page
        {
            public byte[] buffer;
            public int length;
        }
    }
}
