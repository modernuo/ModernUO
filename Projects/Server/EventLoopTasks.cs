/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

    /// <summary>
    /// True when no callbacks are waiting to run.
    /// </summary>
    /// <remarks>
    /// <see cref="ExecuteTasks"/> drains at most <c>_maxPerFrame</c> callbacks, so work can
    /// legitimately be left over. The event loop checks this before sleeping so a backlog keeps
    /// it running instead.
    /// </remarks>
    public bool IsEmpty => _queue.IsEmpty && _priorityQueue.IsEmpty;

    public void Post(Action d, Priority priority = Priority.Normal)
    {
        (priority == Priority.High ? _priorityQueue : _queue).Enqueue(d);
        WakeEventLoop();
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        _queue.Enqueue(() => d(state));
        WakeEventLoop();
    }

    /// <summary>
    /// Nudges the game loop in case it is asleep: the loop blocks on network I/O, which a queue
    /// push alone does not signal.
    /// </summary>
    private void WakeEventLoop()
    {
        // A post from the loop thread cannot need a wake -- the loop is executing this very call
        // -- and the signal is a syscall on every backend.
        if (Thread.CurrentThread == _mainThread)
        {
            EventLoopProfiler.WakeSignal(elided: true);
            return;
        }

        EventLoopProfiler.WakeSignal(elided: false);

        // Safe before networking is configured and after teardown; NetState.Wake does nothing.
        Network.NetState.Wake();
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        if (Thread.CurrentThread == _mainThread)
        {
            d(state);
            return;
        }

        var evt = new AutoResetEvent(false);

        _queue.Enqueue(() =>
        {
            d(state);
            evt.Set();
        });

        WakeEventLoop();

        evt.WaitOne();
    }

    public void ExecuteTasks()
    {
        if (Thread.CurrentThread != _mainThread)
        {
            throw new Exception("Called EventLoop.ExecuteTasks on incorrect thread!");
        }

        var count = _priorityQueue.Count;

        for (var i = 0; i < count; i++)
        {
            if (_priorityQueue.TryDequeue(out var a))
            {
                a();
            }
        }

        count = Math.Min(_queue.Count, _maxPerFrame);

        for (var i = 0; i < count; i++)
        {
            if (_queue.TryDequeue(out var a))
            {
                a();
            }
        }
    }
}
