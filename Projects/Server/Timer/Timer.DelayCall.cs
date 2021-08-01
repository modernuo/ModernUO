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
#if DEBUG_TIMERS
using System.Collections.Generic;
#endif
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server
{
    public partial class Timer
    {
        private static string FormatDelegate(Delegate callback) =>
            callback == null ? "null" : $"{callback.Method.DeclaringType?.FullName ?? ""}.{callback.Method.Name}";

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

#if DEBUG_TIMERS
            DelayCallTimer._stackTraces[t.GetHashCode()] = new StackTrace().ToString();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer StartTimer(Action callback) => StartTimer(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer StartTimer(TimeSpan delay, Action callback) => StartTimer(delay, TimeSpan.Zero, 1, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer StartTimer(TimeSpan delay, TimeSpan interval, Action callback) =>
            StartTimer(delay, interval, 0, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer StartTimer(TimeSpan interval, int count, Action callback) =>
            StartTimer(TimeSpan.Zero, interval, count, callback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer StartTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
        {
            DelayCallTimer t = new DelayCallTimer(delay, interval, count, callback);
            t.Start();
#if DEBUG_TIMERS
            t._allowFinalization = true;
#endif

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(Action callback, out TimerExecutionToken token) =>
            DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, out token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, Action callback, out TimerExecutionToken token) =>
            DelayCall(delay, TimeSpan.Zero, 1, callback, out token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, Action callback, out TimerExecutionToken token) =>
            DelayCall(delay, interval, 0, callback, out token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan interval, int count, Action callback, out TimerExecutionToken token) =>
            DelayCall(TimeSpan.Zero, interval, count, callback, out token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DelayCall(TimeSpan delay, TimeSpan interval, int count, Action callback, out TimerExecutionToken token)
        {
            DelayCallTimer t = DelayCallTimer.GetTimer(delay, interval, count, callback);
            t.Start();

#if DEBUG_TIMERS
            DelayCallTimer._stackTraces[t.GetHashCode()] = new StackTrace().ToString();
#endif
            token = new TimerExecutionToken(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer Pause(TimeSpan ms) => new(ms);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayCallTimer Pause(int ms) => Pause(TimeSpan.FromMilliseconds(ms));

        public sealed class DelayCallTimer : Timer, INotifyCompletion
        {
            private static int _maxPoolSize;
            private static int _baseMaxPoolSize;
            private static int _poolSize;
            private static DelayCallTimer _poolHead;

            public static void Configure()
            {
                _baseMaxPoolSize = ServerConfiguration.GetOrUpdateSetting("timer.maxPoolSize", 1024);
                _maxPoolSize = _baseMaxPoolSize * 16;

                RefillPool(_baseMaxPoolSize, out _poolHead, out _);

                _poolSize = _baseMaxPoolSize;
            }

            internal bool _selfReturn;
#if DEBUG_TIMERS
            internal bool _allowFinalization;
#endif
            private Action _continuation;
            private bool _complete;

            internal DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, Action callback) : base(
                delay,
                interval,
                count
            ) =>
                _continuation = callback;

            internal DelayCallTimer(TimeSpan delay) : base(delay)
            {
#if DEBUG_TIMERS
            t._allowFinalization = true;
#endif
                Start();
            }

            protected override void OnTick()
            {
                _complete = true;
                _continuation?.Invoke();
            }

            public override void Stop()
            {
                base.Stop();

                if (_selfReturn)
                {
                    Return();
                }
            }

            private static void RefillPoolAsync()
            {
                var amountToRefill = Math.Min(_maxPoolSize, _baseMaxPoolSize * 2);
                ThreadPool.UnsafeQueueUserWorkItem(
                    static amount =>
                    {
                        RefillPool(amount, out var head, out var tail);
                        Core.LoopContext.Post(
                            state =>
                            {
                                if (state == null)
                                {
                                    return;
                                }

                                var (listHead, listTail) = ((DelayCallTimer, DelayCallTimer))state;
                                listTail.Attach(_poolHead);
                                _poolHead = listHead;
                                _poolSize += amount;
                                _baseMaxPoolSize = amount;
                            },
                            (head, tail)
                        );
                    },
                    amountToRefill,
                    false
                );
            }

            private static void RefillPool(int amount, out DelayCallTimer head, out DelayCallTimer tail)
            {
#if DEBUG_TIMERS
                logger.Information($"Filling pool with {_maxPoolSize} timers.");
#endif

                DelayCallTimer current = null;
                head = null;
                tail = null;

                for (var i = 0; i < amount; i++)
                {
                    var timer = new DelayCallTimer(TimeSpan.Zero, TimeSpan.Zero, 0, null);
                    timer.Attach(current);

                    if (i == amount - 1)
                    {
                        tail = timer;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            head = timer;
                            tail = timer;
                        }

                        current = timer;
                    }
                }
            }

            internal void Return()
            {
                if (Running)
                {
                    logger.Error($"Timer is returned while still running! {new StackTrace()}");
                    return;
                }

                Version++; // Increment the version so if this is called from OnTick() and another timer is started, we don't have a problem

                if (_poolSize >= _baseMaxPoolSize)
                {
#if DEBUG_TIMERS
                    logger.Warning($"DelayCallTimer pool reached maximum of {_maxPoolSize} timers");
                    _allowFinalization = true;
                    _stackTraces.Remove(GetHashCode());
#endif
                    return;
                }

                Attach(_poolHead);
                _poolHead = this;
                _poolSize++;
                logger.Information($"Timer Pool: {_poolSize}");
            }

            public static DelayCallTimer GetTimer(TimeSpan delay, TimeSpan interval, int count, Action callback)
            {
                if (_poolHead != null)
                {
                    _poolSize--;
                    logger.Information($"Timer Pool: {_poolSize}");
                    var timer = _poolHead;
                    var nextTimer = _poolHead._nextTimer;
                    timer.Detach();
                    _poolHead = (DelayCallTimer)nextTimer;

                    timer.Init(delay, interval, count);
                    timer._continuation = callback;
                    timer._selfReturn = false;
#if DEBUG_TIMERS
                    timer._allowFinalization = false;
#endif

                    return timer;
                }

                RefillPoolAsync();

#if DEBUG_TIMERS
                logger.Warning($"Timer pool depleted and timer was allocated.\n{new StackTrace()});
#endif
                return new DelayCallTimer(delay, interval, count, callback);
            }

            public override string ToString() => $"DelayCallTimer[{FormatDelegate(_continuation)}]";

#if DEBUG_TIMERS
            internal static Dictionary<int, string> _stackTraces = new();

            ~DelayCallTimer()
            {
                if (!_allowFinalization)
                {
                    logger.Warning($"Pooled timer was not returned to the pool.\n{_stackTraces[GetHashCode()]}");
                }
            }
#endif

            public DelayCallTimer GetAwaiter() => this;

            public bool IsCompleted => _complete;

            public void OnCompleted(Action continuation) => _continuation = continuation;

            public void GetResult()
            {
            }
        }
    }
}
