/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EventLoopTasks.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Server;

public sealed class EventLoopContext : SynchronizationContext
{
    public enum Priority
    {
        Normal,
        High
    }

    private readonly ConcurrentQueue<Action> _queue;
    private readonly ConcurrentQueue<Action> _priorityQueue;
    private readonly Thread _mainThread;
    private readonly int _maxPerFrame;

    public EventLoopContext(int maxPerFrame = 128)
    {
        _maxPerFrame = maxPerFrame;
        _queue = [];
        _priorityQueue = [];
        _mainThread = Thread.CurrentThread;
    }

    public override SynchronizationContext CreateCopy() => new EventLoopContext();

    public void Post(Action d, Priority priority = Priority.Normal) =>
        (priority == Priority.High ? _priorityQueue : _queue).Enqueue(d);

    public override void Post(SendOrPostCallback d, object state) => _queue.Enqueue(() => d(state));

    public override void Send(SendOrPostCallback d, object state)
    {
        if (Thread.CurrentThread == _mainThread)
        {
            d(state);
            return;
        }

        AutoResetEvent evt = new AutoResetEvent(false);

        _queue.Enqueue(() =>
        {
            d(state);
            evt.Set();
        });

        evt.WaitOne();
    }

    public void ExecuteTasks()
    {
        if (Thread.CurrentThread != _mainThread)
        {
            throw new Exception("Called EventLoop.ExecuteTasks on incorrect thread!");
        }

        var count = _priorityQueue.Count;

        for (int i = 0; i < count; i++)
        {
            if (_priorityQueue.TryDequeue(out var a))
            {
                a();
            }
        }

        count = Math.Min(_queue.Count, _maxPerFrame);

        for (int i = 0; i < count; i++)
        {
            if (_queue.TryDequeue(out var a))
            {
                a();
            }
        }
    }
}
