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

namespace Server
{
    public partial class Timer
    {
        private static string FormatDelegate(Delegate callback) =>
            callback == null ? "null" : $"{callback.Method.DeclaringType?.FullName ?? ""}.{callback.Method.Name}";

        public static Timer DelayCall(Action callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        public static Timer DelayCall(TimeSpan delay, Action callback) =>
            DelayCall(delay, TimeSpan.Zero, 1, callback);

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, Action callback) =>
            DelayCall(delay, interval, 0, callback);

        public static Timer DelayCall(TimeSpan interval, int count, Action callback) =>
            DelayCall(TimeSpan.Zero, interval, count, callback);

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, Action callback)
        {
            Timer t = new DelayCallTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        private class DelayCallTimer : Timer
        {
            public DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, Action callback) : base(
                delay,
                interval,
                count
            ) => Callback = callback;

            public Action Callback { get; }

            protected override void OnTick()
            {
                Callback?.Invoke();
            }

            public override string ToString() => $"DelayCallTimer[{FormatDelegate(Callback)}]";
        }

        public static Timer DelayCall(Action<Timer> callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

        public static Timer DelayCall(TimeSpan delay, Action<Timer> callback) =>
            DelayCall(delay, TimeSpan.Zero, 1, callback);

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, Action<Timer> callback) =>
            DelayCall(delay, interval, 0, callback);

        public static Timer DelayCall(TimeSpan interval, int count, Action<Timer> callback) =>
            DelayCall(TimeSpan.Zero, interval, count, callback);

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback)
        {
            Timer t = new DelayCallTimerWithTimer(delay, interval, count, callback);
            t.Start();

            return t;
        }

        private class DelayCallTimerWithTimer : Timer
        {
            public DelayCallTimerWithTimer(TimeSpan delay, TimeSpan interval, int count, Action<Timer> callback) : base(
                delay,
                interval,
                count
            ) => Callback = callback;

            public Action<Timer> Callback { get; }

            protected override void OnTick()
            {
                Callback?.Invoke(this);
            }

            public override string ToString() => $"DelayCallTimer[{FormatDelegate(Callback)}]";
        }
    }
}
