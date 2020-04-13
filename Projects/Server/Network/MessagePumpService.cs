
/***************************************************************************
 *                               MessagePump.cs
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

using System.Buffers;
using System.Collections.Concurrent;

namespace Server.Network
{
  public interface IMessagePumpService
  {
    void QueueWork(NetState ns, IMemoryOwner<byte> memOwner, OnPacketReceive onReceive);
    void DoWork();
  }

  public class MessagePumpService : IMessagePumpService
  {
    private readonly ConcurrentQueue<Work> m_WorkQueue = new ConcurrentQueue<Work>();

    public void QueueWork(NetState ns, IMemoryOwner<byte> memOwner, OnPacketReceive onReceive)
    {
      m_WorkQueue.Enqueue(new Work(ns, memOwner, onReceive));
      Core.Set();
    }

    public void DoWork()
    {
      int count = 0;
      while (!m_WorkQueue.IsEmpty && count++ < 250)
      {
        if (!m_WorkQueue.TryDequeue(out Work work))
          break;

        work.OnReceive(work.State, new PacketReader(new ReadOnlySequence<byte>(work.MemoryOwner.Memory)));
        work.MemoryOwner.Dispose();
      }
    }

    private class Work
    {
      public readonly NetState State;
      // TODO: Force dispose?
      public readonly IMemoryOwner<byte> MemoryOwner;
      public readonly OnPacketReceive OnReceive;

      public Work(NetState ns, IMemoryOwner<byte> memOwner, OnPacketReceive onReceive)
      {
        State = ns;
        MemoryOwner = memOwner;
        OnReceive = onReceive;
      }
    }
  }
}
