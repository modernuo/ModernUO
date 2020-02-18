using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Utilities
{
  public interface IRef : IDisposable { }

  public class RefPool<TRef> where TRef : IRef
  {
    private ConcurrentBag<TRef> m_Queues = new ConcurrentBag<TRef>();
    private Func<RefPool<TRef>, TRef> m_Generator;
    public int MaxQueueRetain { get; set; } = 10;

    public RefPool(Func<RefPool<TRef>, TRef> generator)
    {
      m_Generator = generator;
    }

    public TRef Get()
    {
      if (m_Queues.TryTake(out TRef item))
        return item;
      return m_Generator(this);
    }

    internal void Return(TRef queueRef)
    {
      if (m_Queues.Count < MaxQueueRetain)
        m_Queues.Add(queueRef);
    }
  }
  public class QueueRef<T> : Queue<T>, IRef
  {
    private RefPool<QueueRef<T>> _pool;
    internal QueueRef(RefPool<QueueRef<T>> pool)
    {
      _pool = pool;
    }
    public void Dispose()
    {
      Clear();
      _pool.Return(this);
    }

    public static QueueRef<T> CreateInstance(RefPool<QueueRef<T>> pool) => new QueueRef<T>(pool);
  }
  public class StackRef<T> : Stack<T>, IRef
  {
    private RefPool<StackRef<T>> _pool;
    internal StackRef(RefPool<StackRef<T>> pool)
    {
      _pool = pool;
    }
    public void Dispose()
    {
      Clear();
      _pool.Return(this);
    }
    public static StackRef<T> CreateInstance(RefPool<StackRef<T>> pool) => new StackRef<T>(pool);
  }
}
