/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: TimerDelayCalls.cs - Created: 2020/07/31 - Updated: 2020/07/31  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server
{
  public delegate void TimerCallback();
  public delegate void TimerStateCallback<in T>(T state);
  public delegate void TimerStateCallback<in T1, in T2>(T1 t1, T2 t2);
  public delegate void TimerStateCallback<in T1, in T2, in T3>(T1 t1, T2 t2, T3 t3);
  public delegate void TimerStateCallback<in T1, in T2, in T3, in T4>(T1 t1, T2 t2, T3 t3, T4 t4);

  public partial class Timer
  {
    public static Timer DelayCall(TimerCallback callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

    public static Timer DelayCall(TimeSpan delay, TimerCallback callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

    public static Timer DelayCall(TimeSpan delay, TimeSpan interval, TimerCallback callback) =>
      DelayCall(delay, interval, 0, callback);

    public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
    {
      Timer t = new DelayCallTimer(delay, interval, count, callback);

      t.Priority = ComputePriority(count == 1 ? delay : interval);
      t.Start();

      return t;
    }

    private class DelayCallTimer : Timer
    {
      public DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback) : base(delay,
        interval, count)
      {
        Callback = callback;
        RegCreation();
      }

      public TimerCallback Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke();
      }

      public override string ToString() => $"DelayCallTimer[{FormatDelegate(Callback)}]";
    }

    public static Timer DelayCall<T>(TimerStateCallback<T> callback, T state) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimerStateCallback<T> callback, T state) =>
      DelayCall(delay, TimeSpan.Zero, 1, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T> callback, T state) =>
      DelayCall(delay, interval, 0, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T> callback,
      T state)
    {
      Timer t = new DelayStateCallTimer<T>(delay, interval, count, callback, state);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    private class DelayStateCallTimer<T> : Timer
    {
      private readonly T m_State;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T> callback, T state)
        : base(delay, interval, count)
      {
        Callback = callback;
        m_State = state;

        RegCreation();
      }

      public TimerStateCallback<T> Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_State);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    public static Timer DelayCall<T1, T2>(TimerStateCallback<T1, T2> callback, T1 t1, T2 t2) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimerStateCallback<T1, T2> callback, T1 t1, T2 t2) =>
      DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T1, T2> callback,
      T1 t1, T2 t2) => DelayCall(delay, interval, 0, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2> callback,
      T1 t1, T2 t2)
    {
      Timer t = new DelayStateCallTimer<T1, T2>(delay, interval, count, callback, t1, t2);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    private class DelayStateCallTimer<T1, T2> : Timer
    {
      private readonly T1 m_T1;
      private readonly T2 m_T2;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2> callback,
        T1 t1, T2 t2) : base(delay, interval, count)
      {
        Callback = callback;
        m_T1 = t1;
        m_T2 = t2;

        RegCreation();
      }

      public TimerStateCallback<T1, T2> Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_T1, m_T2);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    public static Timer DelayCall<T1, T2, T3>(TimerStateCallback<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3);

    public static Timer DelayCall<T1, T2, T3>(TimeSpan delay, TimerStateCallback<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3) =>
      DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2, t3);

    public static Timer DelayCall<T1, T2, T3>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T1, T2, T3> callback,
      T1 t1, T2 t2, T3 t3) => DelayCall(delay, interval, 0, callback, t1, t2, t3);

    public static Timer DelayCall<T1, T2, T3>(TimeSpan delay, TimeSpan interval, int count,
      TimerStateCallback<T1, T2, T3> callback, T1 t1, T2 t2, T3 t3)
    {
      Timer t = new DelayStateCallTimer<T1, T2, T3>(delay, interval, count, callback, t1, t2, t3);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    private class DelayStateCallTimer<T1, T2, T3> : Timer
    {
      private readonly T1 m_T1;
      private readonly T2 m_T2;
      private readonly T3 m_T3;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2, T3> callback,
        T1 t1, T2 t2, T3 t3) : base(delay, interval, count)
      {
        Callback = callback;
        m_T1 = t1;
        m_T2 = t2;
        m_T3 = t3;

        RegCreation();
      }

      public TimerStateCallback<T1, T2, T3> Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_T1, m_T2, m_T3);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    public static Timer DelayCall<T1, T2, T3, T4>(TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimerStateCallback<T1, T2, T3, T4> callback,
      T1 t1, T2 t2, T3 t3, T4 t4) => DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimeSpan interval,
      TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4) =>
      DelayCall(delay, interval, 0, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimeSpan interval, int count,
      TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4)
    {
      Timer t = new DelayStateCallTimer<T1, T2, T3, T4>(delay, interval, count, callback, t1, t2, t3, t4);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    private class DelayStateCallTimer<T1, T2, T3, T4> : Timer
    {
      private readonly T1 m_T1;
      private readonly T2 m_T2;
      private readonly T3 m_T3;
      private readonly T4 m_T4;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2, T3, T4> callback,
        T1 t1, T2 t2, T3 t3, T4 t4) : base(delay, interval, count)
      {
        Callback = callback;
        m_T1 = t1;
        m_T2 = t2;
        m_T3 = t3;
        m_T4 = t4;

        RegCreation();
      }

      public TimerStateCallback<T1, T2, T3, T4> Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_T1, m_T2, m_T3, m_T4);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }
  }
}
