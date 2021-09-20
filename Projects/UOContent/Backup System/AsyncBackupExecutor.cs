using System;
using System.Threading;

namespace Server.Backup
{
    public class AsyncBackupExecutor : Timer
    {
        private int _executed;
        private readonly DateTime _date;
        public event Action<DateTime> AsyncActions;
        public bool Executed => _executed != 0;

        public AsyncBackupExecutor(DateTime date) : base(TimeSpan.Zero)
        {
            _date = date;
            Start();
        }

        protected override void OnTick()
        {
            ThreadPool.UnsafeQueueUserWorkItem(
                _ =>
                {
                    AsyncActions?.Invoke(_date);
                    Interlocked.Exchange(ref _executed, 1);
                }, null);
        }
    }
}
