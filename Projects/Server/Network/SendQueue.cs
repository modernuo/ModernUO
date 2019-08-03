/***************************************************************************
 *                               SendQueue.cs
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

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Server.Network
{
  public class SendQueue<T>
  {
    private BlockingCollection<T> m_Queue = new BlockingCollection<T>(new ConcurrentQueue<T>());

    public SendQueue()
    {
    }

    public void Enqueue(T t)
    {
      m_Queue.Add(t);
    }

    public Task<T> DequeueAsync()
    {
      TaskCompletionSource<T> taskCompletion = new TaskCompletionSource<T>();
      Task.Run(() => taskCompletion.SetResult(Dequeue()));
      return taskCompletion.Task;
    }

    public T Dequeue()
    {
      return m_Queue.Take();
    }
  }
}
