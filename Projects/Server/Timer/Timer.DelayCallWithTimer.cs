/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.DelayCallWithTimer.cs                                     *
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

            static DelayCallTimerWithTimer()
            {
                for (var i = 0; i < _maxPoolSize; i++)
                {
                    var timer = new DelayCallTimerWithTimer(TimeSpan.Zero, TimeSpan.Zero, 0, null);
                    timer.Attach(_poolHead);
                    _poolHead = timer;
                }
            }

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
