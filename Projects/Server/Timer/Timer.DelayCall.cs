/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Timer.DelayCall.cs                                              *
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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Server
{
    public partial class Timer
    {
        private static string FormatDelegate(Delegate callback) =>
            callback == null ? "null" : $"{callback.Method.DeclaringType?.FullName ?? ""}.{callback.Method.Name}";

        public abstract class PooledTimer : Timer
        {
            internal bool _selfReturn;
            internal bool _allowFinalization;

            internal PooledTimer(TimeSpan delay) : base(delay)
            {
            }

            internal PooledTimer(TimeSpan delay, TimeSpan interval, int count = 0) : base(delay, interval, count)
            {}

            public abstract void Return();

            public override void Stop()
            {
                base.Stop();

                if (_selfReturn)
                {
                    Return();
                }
            }

#if DEBUG
            ~PooledTimer()
            {
                if (!_allowFinalization)
                {
                    logger.Warning($"{this} was not returned to the pool.\n{new StackTrace()}");
                }
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(Action callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, Action callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, Action callback) =>
            DelayCall(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan interval, int count, Action callback) =>
            DelayCall(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, int count, Action callback)
        {
            DelayCallTimer t = DelayCallTimer.GetTimer(delay, interval, count, callback);
            t._selfReturn = true;
            t.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallWithTimer(Action callback) =>
            DelayCallWithTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallWithTimer(TimeSpan delay, Action callback) =>
            DelayCallWithTimer(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, Action callback) =>
            DelayCallWithTimer(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallWithTimer(TimeSpan interval, int count, Action callback) =>
            DelayCallWithTimer(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
        {
            DelayCallTimer t = DelayCallTimer.GetTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        public sealed class DelayCallTimer : PooledTimer, INotifyCompletion
        {
            private static int _maxPoolSize = 1024;
            private static int _poolSize;
            private static DelayCallTimer _poolHead;

            private Action _continuation;
            private bool _complete;

            internal DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, Action callback) : base(
                delay,
                interval,
                count
            ) => _continuation = callback;

            internal DelayCallTimer(TimeSpan delay) : base(delay) => Start();

            protected override void OnTick()
            {
                _complete = true;
                _continuation?.Invoke();
            }

            public override void Return()
            {
                if (Running)
                {
                    logger.Error($"Timer is returned while still running! {new StackTrace()}");
                    return;
                }

                if (_poolSize > _maxPoolSize)
                {
                    logger.Debug($"DelayCallTimer pool reached maximum of {_maxPoolSize} timers");
                    _allowFinalization = true;
                    return;
                }

                Attach(_poolHead);
                _poolHead = this;
                _poolSize++;
            }

            public static DelayCallTimer GetTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
            {
                if (_poolHead != null)
                {
                    _poolSize--;
                    var timer = _poolHead;
                    timer.Detach();
                    timer.Init(delay, interval, count);
                    timer._continuation = callback;
                    timer._selfReturn = false;

                    return timer;
                }

                logger.Debug($"DelayCallTimer pool depleted and timer was allocated.");
                return new DelayCallTimer(delay, interval, count, callback);
            }

            public override string ToString() => $"DelayCallTimer[{FormatDelegate(_continuation)}]";

            public DelayCallTimer GetAwaiter() => this;

            public bool IsCompleted => _complete;

            public void OnCompleted(Action continuation) => _continuation = continuation;

            public void GetResult()
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer Pause(TimeSpan ms) => new(ms);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer Pause(int ms) => Pause(TimeSpan.FromMilliseconds(ms));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(Action<Timer> callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, Action<Timer> callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCall(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCall(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            DelayCallTimerWithTimer t = DelayCallTimerWithTimer.GetTimer(delay, interval, count, callback);
            t._selfReturn = true;
            t.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimerWithTimer DelayCallWithTimer(Action<Timer> callback) =>
            DelayCallWithTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, Action<Timer> callback) =>
            DelayCallWithTimer(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCallWithTimer(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCallWithTimer(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            DelayCallTimerWithTimer t = DelayCallTimerWithTimer.GetTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        public sealed class DelayCallTimerWithTimer : PooledTimer
        {
            private static int _maxPoolSize = 1024;
            private static int _poolSize;
            private static DelayCallTimerWithTimer _poolHead;
            private Action<Timer> _callback;

            internal DelayCallTimerWithTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback) : base(
                delay,
                interval,
                count
            ) => _callback = callback;

            protected override void OnTick() => _callback?.Invoke(this);

            public override void Return()
            {
                if (Running)
                {
                    logger.Error($"Timer is returned while still running! {new StackTrace()}");
                    return;
                }

                if (_poolSize > _maxPoolSize)
                {
                    logger.Debug($"DelayCallTimerWithTimer pool reached maximum of {_maxPoolSize} timers");
                    _allowFinalization = true;
                    return;
                }

                Attach(_poolHead);
                _poolHead = this;
                _poolSize++;
            }

            public static DelayCallTimerWithTimer GetTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
            {
                if (_poolHead != null)
                {
                    _poolSize--;
                    var timer = _poolHead;
                    timer.Detach();
                    timer.Init(delay, interval, count);
                    timer._callback = callback;
                    timer._selfReturn = false;

                    return timer;
                }

                logger.Debug($"DelayCallTimerWithTimer pool depleted and timer was allocated.");
                return new DelayCallTimerWithTimer(delay, interval, count, callback);
            }

            public override string ToString() => $"DelayCallTimerWithTimer[{FormatDelegate(_callback)}]";
        }
    }
}
