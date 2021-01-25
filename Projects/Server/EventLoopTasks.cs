using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Server
{
    public sealed class EventLoopContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<Action> _queue;
        private readonly Thread _mainThread;

        public EventLoopContext()
        {
            _queue = new ConcurrentQueue<Action>();
            _mainThread = Thread.CurrentThread;
        }

        public override SynchronizationContext CreateCopy() => new EventLoopContext();

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

        public int ExecuteTasks()
        {
            if (Thread.CurrentThread != _mainThread)
            {
                throw new Exception("Called EventLoop.ExecuteTasks on incorrect thread!");
            }

            var count = _queue.Count;

            for (int i = 0; i < count; i++)
            {
                if (!_queue.TryDequeue(out var a))
                {
                    return count;
                }

                a();
            }

            return count;
        }
    }
}
