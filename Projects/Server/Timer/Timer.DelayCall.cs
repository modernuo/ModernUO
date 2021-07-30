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
            public abstract void Return();

            internal PooledTimer(TimeSpan delay) : base(delay)
            {
            }

            internal PooledTimer(TimeSpan delay, TimeSpan interval, int count = 0) : base(delay, interval, count)
            {
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimer timer, Action callback) =>
            DelayCallInit(ref timer, TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimer timer, TimeSpan delay, Action callback) =>
            DelayCallInit(ref timer, delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimer timer, TimeSpan delay, TimeSpan interval, Action callback) =>
            DelayCallInit(ref timer, delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimer timer, TimeSpan interval, int count, Action callback) =>
            DelayCallInit(ref timer, TimeSpan.Zero, interval, count, callback);

        public static void DelayCallInit(ref DelayCallTimer timer, TimeSpan delay, TimeSpan interval, int count, Action callback)
        {
            if (timer == null)
            {
                timer = DelayCallWithTimer(delay, interval, count, callback);
            }
            else
            {
                timer.Stop();
                timer.Init(delay, interval, count, callback);
                timer.Start();
            }
        }

        public sealed class DelayCallTimer : PooledTimer
        {
            private static DelayCallTimer _poolHead;
            public static int PoolOffset { get; private set; }

            private Action _continuation;
            private bool _complete;

            internal DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, Action callback) : base(
                delay,
                interval,
                count
            )
            {
                _complete = true;
                _continuation = callback;
            }

            internal DelayCallTimer(TimeSpan delay) : base(delay) => Start();

            public void Init(Action callback) => Init(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, Action callback) => Init(delay, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, TimeSpan interval, Action callback) => Init(delay, interval, 0, callback);

            public void Init(TimeSpan interval, int count, Action callback) =>
                Init(TimeSpan.Zero, interval, count, callback);

            public void Init(TimeSpan delay, TimeSpan interval, int count, Action callback)
            {
                Init(delay, interval, count);
                _continuation = callback;
            }

            protected override void OnTick() => _continuation?.Invoke();

            public override void Return()
            {
                if (Running)
                {
                    logger.Error($"Timer is returned while still running! {new StackTrace()}");
                    return;
                }

                Attach(_poolHead);
                _poolHead = this;
            }

            public static DelayCallTimer GetTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
            {
                if (_poolHead != null)
                {
                    var timer = _poolHead;
                    timer.Detach();
                    timer.Init(delay, interval, count, callback);

                    return timer;
                }

                PoolOffset--;
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

        public static void DelayCall(Action<Timer> callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        public static void DelayCall(TimeSpan delay, Action<Timer> callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

        public static void DelayCall(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCall(delay, interval, 0, callback);

        public static void DelayCall(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCall(TimeSpan.Zero, interval, count, callback);

        public static void DelayCall(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            DelayCallTimerWithTimer t = DelayCallTimerWithTimer.GetTimer(delay, interval, count, callback);
            t.Start();
        }

        public static DelayCallTimerWithTimer DelayCallWithTimer(Action<Timer> callback) =>
            DelayCallWithTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, Action<Timer> callback) =>
            DelayCallWithTimer(delay, TimeSpan.Zero, 1, callback);

        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCallWithTimer(delay, interval, 0, callback);

        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCallWithTimer(TimeSpan.Zero, interval, count, callback);

        public static DelayCallTimerWithTimer DelayCallWithTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            DelayCallTimerWithTimer t = DelayCallTimerWithTimer.GetTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        public sealed class DelayCallTimerWithTimer : PooledTimer
        {
            private static DelayCallTimerWithTimer _poolHead;
            public static int PoolOffset { get; private set; }

            internal DelayCallTimerWithTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback) : base(
                delay,
                interval,
                count
            ) => Callback = callback;

            public Action<Timer> Callback { get; private set; }

            public void Init(Action<Timer> callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, Action<Timer> callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
                Init(delay, interval, 0, callback);

            public void Init(TimeSpan interval, int count, Action<Timer> callback) =>
                Init(TimeSpan.Zero, interval, count, callback);

            public void Init(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
            {
                Init(delay, interval, count);
                Callback = callback;
            }

            protected override void OnTick()
            {
                Callback?.Invoke(this);
            }

            public override void Return()
            {
                if (Running)
                {
                    logger.Error($"Timer is returned while still running! {new StackTrace()}");
                    return;
                }

                Attach(_poolHead);
                _poolHead = this;
            }

            public static DelayCallTimerWithTimer GetTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
            {
                if (_poolHead != null)
                {
                    var timer = _poolHead;
                    timer.Detach();
                    timer.Init(delay, interval, count, callback);

                    return timer;
                }

                PoolOffset--;
                return new DelayCallTimerWithTimer(delay, interval, count, callback);
            }

            public override string ToString() => $"DelayCallTimerWithTimer[{FormatDelegate(Callback)}]";
        }
    }
}
