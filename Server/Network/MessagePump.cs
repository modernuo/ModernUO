using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Server.Network
{
  public class MessagePump
  {
    private ConcurrentQueue<Work> m_WorkQueue = new ConcurrentQueue<Work>();
    public Listener[] Listeners => new Listener[0];

    public void AddListener(IPEndPoint ipep)
    {
      Listener[] listeners = new Listener[Listeners.Length + 1];
      Array.Copy(Listeners, listeners, Listeners.Length);
      Listener listener = new Listener(ipep);
      _ = listener.Start(this);
      listeners[Listeners.Length] = listener;
    }

    public void QueueWork(NetState ns, in ReadOnlySequence<byte> seq, OnPacketReceive onReceive)
    {
      m_WorkQueue.Enqueue(new Work(ns, seq, onReceive));
      Core.Set();
    }

    public void DoWork()
    {
      int count = m_WorkQueue.Count;
      while (count-- > 0)
      {
        if (!m_WorkQueue.TryDequeue(out Work work))
          break;

        work.OnReceive(work.State, new PacketReader(work.Sequence));
      }
    }

    // TODO: Optimize this with a pool
    private class Work
    {
      public NetState State;
      public ReadOnlySequence<byte> Sequence;
      public OnPacketReceive OnReceive;

      public Work(NetState ns, in ReadOnlySequence<byte> seq, OnPacketReceive onReceive)
      {
        State = ns;
        Sequence = seq;
        OnReceive = onReceive;
      }
    }
  }
}
