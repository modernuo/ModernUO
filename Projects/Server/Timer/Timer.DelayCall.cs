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
        public static DelayCallTimer DelayCallReturnTimer(Action callback) =>
            DelayCallReturnTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallReturnTimer(TimeSpan delay, Action callback) =>
            DelayCallReturnTimer(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallReturnTimer(TimeSpan delay, TimeSpan interval, Action callback) =>
            DelayCallReturnTimer(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallReturnTimer(TimeSpan interval, int count, Action callback) =>
            DelayCallReturnTimer(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer DelayCallReturnTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
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
                timer = DelayCallReturnTimer(delay, interval, count, callback);
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

            internal DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, Action callback) : base(
                delay,
                interval,
                count
            ) => Callback = callback;

            public Action Callback { get; private set; }

            public void Init(Action callback) => Init(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, Action callback) => Init(delay, TimeSpan.Zero, 1, callback);

            public void Init(TimeSpan delay, TimeSpan interval, Action callback) => Init(delay, interval, 0, callback);

            public void Init(TimeSpan interval, int count, Action callback) =>
                Init(TimeSpan.Zero, interval, count, callback);

            public void Init(TimeSpan delay, TimeSpan interval, int count, Action callback)
            {
                Init(delay, interval, count);
                Callback = callback;
            }

            protected override void OnTick()
            {
                Callback?.Invoke();
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

            public override string ToString() => $"DelayCallTimer[{FormatDelegate(Callback)}]";
        }

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

        public static DelayCallTimerWithTimer DelayCallReturnTimer(Action<Timer> callback) =>
            DelayCallReturnTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        public static DelayCallTimerWithTimer DelayCallReturnTimer(TimeSpan delay, Action<Timer> callback) =>
            DelayCallReturnTimer(delay, TimeSpan.Zero, 1, callback);

        public static DelayCallTimerWithTimer DelayCallReturnTimer(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCallReturnTimer(delay, interval, 0, callback);

        public static DelayCallTimerWithTimer DelayCallReturnTimer(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCallReturnTimer(TimeSpan.Zero, interval, count, callback);

        public static DelayCallTimerWithTimer DelayCallReturnTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            DelayCallTimerWithTimer t = DelayCallTimerWithTimer.GetTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimerWithTimer timer, Action<Timer> callback) =>
            DelayCallInit(ref timer, TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimerWithTimer timer, TimeSpan delay, Action<Timer> callback) =>
            DelayCallInit(ref timer, delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimerWithTimer timer, TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCallInit(ref timer, delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCallInit(ref DelayCallTimerWithTimer timer, TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCallInit(ref timer, TimeSpan.Zero, interval, count, callback);

        public static void DelayCallInit(ref DelayCallTimerWithTimer timer, TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            if (timer == null)
            {
                timer = DelayCallReturnTimer(delay, interval, count, callback);
            }
            else
            {
                timer.Stop();
                timer.Init(delay, interval, count, callback);
                timer.Start();
            }
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
