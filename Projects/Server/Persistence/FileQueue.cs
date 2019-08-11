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
    private static int bufferSize;

    private Chunk[] active;
    private int activeCount;
    private Page buffered;

    private FileCommitCallback callback;

    private ManualResetEvent idle;

    private Queue<Page> pending;

    private object syncRoot;

    static FileQueue()
    {
      bufferSize = FileOperations.BufferSize;
    }

    public FileQueue(int concurrentWrites, FileCommitCallback callback)
    {
      if (concurrentWrites < 1) throw new ArgumentOutOfRangeException("concurrentWrites");

      if (bufferSize < 1)
        throw new ArgumentOutOfRangeException("bufferSize");

      syncRoot = new object();

      active = new Chunk[concurrentWrites];
      pending = new Queue<Page>();

      this.callback = callback;

      idle = new ManualResetEvent(true);
    }

    public long Position{ get; private set; }

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

        for (int slot = 0; slot < active.Length; ++slot)
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

    private void Commit(Chunk chunk, int slot)
    {
      if (slot < 0 || slot >= active.Length) throw new ArgumentOutOfRangeException("slot");

      lock (syncRoot)
      {
        if (active[slot] != chunk) throw new ArgumentException();

        ArrayPool<byte>.Shared.Return(chunk.Buffer, true);

        if (pending.Count > 0)
        {
          Page page = pending.Dequeue();

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
      if (buffer.Length - offset < size) throw new ArgumentException();

      Position += size;

      while (size > 0)
      {
        if (buffered.buffer == null)
          buffered.buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        byte[] page = buffered.buffer; // buffer page
        int pageSpace = page.Length - buffered.length; // available bytes in page
        int byteCount = size > pageSpace ? pageSpace : size; // how many bytes we can copy over

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
      private int offset;
      private FileQueue owner;
      private int slot;

      public Chunk(FileQueue owner, int slot, byte[] buffer, int offset, int size)
      {
        this.owner = owner;
        this.slot = slot;

        Buffer = buffer;
        this.offset = offset;
        Size = size;
      }

      public byte[] Buffer{ get; }

      public int Offset => 0;

      public int Size{ get; }

      public void Commit()
      {
        owner.Commit(this, slot);
      }
    }

    private struct Page
    {
      public byte[] buffer;
      public int length;
    }
  }
}
