using System;

namespace Server
{
  public partial class Timer
  {
    public delegate void TimerStateCallback<in T1, in T2, in T3, in T4>(T1 t1, T2 t2, T3 t3, T4 t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4) =>
      DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T1, T2, T3, T4> callback, T1 t1, T2 t2, T3 t3, T4 t4) =>
      DelayCall(delay, interval, 0, callback, t1, t2, t3, t4);

    public static Timer DelayCall<T1, T2, T3, T4>(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2, T3, T4> callback,
      T1 t1, T2 t2, T3 t3, T4 t4)
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
