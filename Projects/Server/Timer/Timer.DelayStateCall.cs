/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Timer.DelayStateCall.cs                                         *
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
using System.Runtime.CompilerServices;

namespace Server;

public partial class Timer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T>(Action<T> callback, T state) =>
        DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T>(TimeSpan delay, Action<T> callback, T state) =>
        DelayCall(delay, TimeSpan.Zero, 1, callback, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, Action<T> callback, T state) =>
        DelayCall(delay, interval, 0, callback, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, int count, Action<T> callback, T state) =>
        new DelayStateCallTimer<T>(delay, interval, count, callback, state).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2>(Action<T1, T2> callback, T1 t1, T2 t2) =>
        DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2>(TimeSpan delay, Action<T1, T2> callback, T1 t1, T2 t2) =>
        DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimeSpan interval, Action<T1, T2> callback, T1 t1, T2 t2) =>
        DelayCall(delay, interval, 0, callback, t1, t2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2>(
        TimeSpan delay, TimeSpan interval, int count, Action<T1, T2> callback,
        T1 t1, T2 t2
    ) => new DelayStateCallTimer<T1, T2>(delay, interval, count, callback, t1, t2).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3>(Action<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3) =>
        DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3>(
        TimeSpan delay, Action<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3
    ) => DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2, t3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3>(
        TimeSpan delay, TimeSpan interval, Action<T1, T2, T3> callback,
        T1 t1, T2 t2, T3 t3
    ) => DelayCall(delay, interval, 0, callback, t1, t2, t3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3>(
        TimeSpan delay, TimeSpan interval, int count,
        Action<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3
    ) => new DelayStateCallTimer<T1, T2, T3>(delay, interval, count, callback, t1, t2, t3).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4>(
        Action<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4
    ) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4>(
        TimeSpan delay, Action<T1, T2, T3, T4> callback,
        T1 t1, T2 t2, T3 t3, T4 t4
    ) => DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4>(
        TimeSpan delay, TimeSpan interval,
        Action<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4
    ) => DelayCall(delay, interval, 0, callback, t1, t2, t3, t4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4>(
        TimeSpan delay, TimeSpan interval, int count,
        Action<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4
    ) => new DelayStateCallTimer<T1, T2, T3, T4>(delay, interval, count, callback, t1, t2, t3, t4).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4, T5>(
        Action<T1, T2, T3, T4, T5> callback, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5
    ) => new DelayStateCallTimer<T1, T2, T3, T4, T5>(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3, t4, t5).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4, T5>(
        TimeSpan delay,
        Action<T1, T2, T3, T4, T5> callback, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5
    ) => new DelayStateCallTimer<T1, T2, T3, T4, T5>(delay, TimeSpan.Zero, 1, callback, t1, t2, t3, t4, t5).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4, T5>(
        TimeSpan delay, TimeSpan interval,
        Action<T1, T2, T3, T4, T5> callback, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5
    ) => new DelayStateCallTimer<T1, T2, T3, T4, T5>(delay, interval, 0, callback, t1, t2, t3, t4, t5).Start();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer DelayCall<T1, T2, T3, T4, T5>(
        TimeSpan delay, TimeSpan interval, int count,
        Action<T1, T2, T3, T4, T5> callback, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5
    ) => new DelayStateCallTimer<T1, T2, T3, T4, T5>(delay, interval, count, callback, t1, t2, t3, t4, t5).Start();

    private class DelayStateCallTimer<T> : Timer
    {
        private readonly T _t1;

        public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, Action<T> callback, T state)
            : base(delay, interval, count)
        {
            Callback = callback;
            _t1 = state;
        }

        public Action<T> Callback { get; }

        protected override void OnTick() => Callback?.Invoke(_t1);

        public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    private class DelayStateCallTimer<T1, T2> : Timer
    {
        private readonly T1 _t1;
        private readonly T2 _t2;

        public DelayStateCallTimer(
            TimeSpan delay, TimeSpan interval, int count, Action<T1, T2> callback,
            T1 t1, T2 t2
        ) : base(delay, interval, count)
        {
            Callback = callback;
            _t1 = t1;
            _t2 = t2;
        }

        public Action<T1, T2> Callback { get; }

        protected override void OnTick() => Callback?.Invoke(_t1, _t2);

        public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    private class DelayStateCallTimer<T1, T2, T3> : Timer
    {
        private readonly T1 _t1;
        private readonly T2 _t2;
        private readonly T3 _t3;

        public DelayStateCallTimer(
            TimeSpan delay, TimeSpan interval, int count, Action<T1, T2, T3> callback,
            T1 t1, T2 t2, T3 t3
        ) : base(delay, interval, count)
        {
            Callback = callback;
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
        }

        public Action<T1, T2, T3> Callback { get; }

        protected override void OnTick() => Callback?.Invoke(_t1, _t2, _t3);

        public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    private class DelayStateCallTimer<T1, T2, T3, T4> : Timer
    {
        private readonly T1 _t1;
        private readonly T2 _t2;
        private readonly T3 _t3;
        private readonly T4 _t4;

        public DelayStateCallTimer(
            TimeSpan delay, TimeSpan interval, int count, Action<T1, T2, T3, T4> callback,
            T1 t1, T2 t2, T3 t3, T4 t4
        ) : base(delay, interval, count)
        {
            Callback = callback;
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
            _t4 = t4;
        }

        public Action<T1, T2, T3, T4> Callback { get; }

        protected override void OnTick() => Callback?.Invoke(_t1, _t2, _t3, _t4);

        public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    private class DelayStateCallTimer<T1, T2, T3, T4, T5> : Timer
    {
        private readonly T1 _t1;
        private readonly T2 _t2;
        private readonly T3 _t3;
        private readonly T4 _t4;
        private readonly T5 _t5;

        public DelayStateCallTimer(
            TimeSpan delay, TimeSpan interval, int count, Action<T1, T2, T3, T4, T5> callback,
            T1 t1, T2 t2, T3 t3, T4 t4, T5 t5
        ) : base(delay, interval, count)
        {
            Callback = callback;
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
            _t4 = t4;
            _t5 = t5;
        }

        public Action<T1, T2, T3, T4, T5> Callback { get; }

        protected override void OnTick() => Callback?.Invoke(_t1, _t2, _t3, _t4, _t5);

        public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }
}
